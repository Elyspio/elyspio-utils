using System.Diagnostics;
using Elyspio.Utils.Results;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Elyspio.Utils.Telemetry.Tests;

public sealed class TraceScopeTests : IDisposable
{
	private readonly List<Activity> _stopped = [];
	private readonly ActivityListener _listener;

	public TraceScopeTests()
	{
		_listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == typeof(TraceScopeTests).Assembly.GetName().Name,
			Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				lock (_stopped) _stopped.Add(activity);
			},
		};
		ActivitySource.AddActivityListener(_listener);
	}

	public void Dispose() => _listener.Dispose();

	private List<Activity> Spans(string method)
	{
		lock (_stopped) return _stopped.Where(a => a.OperationName.EndsWith($".{method}", StringComparison.Ordinal)).ToList();
	}

	public sealed record ItemMissing(string Id) : NotFound("items.missing", "Item not found.");

	public sealed class Service(ILogger<Service> logger)
	{
		public Result<int> Find(string id)
		{
			using var trace = logger.Trace($"{Log.F(id)}");

			if (id == "missing") return trace.Fail(new ItemMissing(id));

			trace.Exit($"found {id}");
			return 42;
		}

		public Result<int> Relay(Result<int> inner)
		{
			using var trace = logger.Trace();

			return trace.Return(inner);
		}

		public void Crash()
		{
			using var trace = logger.Trace();

			trace.LogError(new InvalidOperationException("bug"), $"failed");
		}
	}

	[Fact]
	public void Success_logs_enter_and_exit_and_tags_the_span()
	{
		var logger = new TestLogger<Service>();

		new Service(logger).Find("a").IsSuccess.ShouldBeTrue();

		var span = Spans("Find").Single();
		span.Status.ShouldBe(ActivityStatusCode.Unset);
		span.GetTagItem("code.arguments").ShouldBe("id=\"a\"");
		span.GetTagItem("code.exit").ShouldBe("found a");
		logger.Entries[0].Message.ShouldContain("Find -- Enter -- id=\"a\"");
		logger.Entries[1].Message.ShouldContain("Find -- Exit -- found a");
	}

	[Fact]
	public void Fail_marks_the_span_and_the_exit_log()
	{
		var logger = new TestLogger<Service>();

		new Service(logger).Find("missing").IsFailure.ShouldBeTrue();

		var span = Spans("Find").Single();
		span.Status.ShouldBe(ActivityStatusCode.Error);
		span.GetTagItem("error.type").ShouldBe("items.missing");
		logger.Entries[^1].Message.ShouldContain("failed: items.missing");
	}

	[Fact]
	public void Return_marks_a_relayed_failure_only()
	{
		var service = new Service(new TestLogger<Service>());

		service.Relay(new ItemMissing("a"));
		service.Relay(1);

		Spans("Relay").Select(a => a.Status).ShouldBe([ActivityStatusCode.Error, ActivityStatusCode.Unset]);
	}

	[Fact]
	public void Logged_exception_marks_the_span()
	{
		new Service(new TestLogger<Service>()).Crash();

		Spans("Crash").Single().Status.ShouldBe(ActivityStatusCode.Error);
	}

	[Fact]
	public void Disabled_level_evaluates_no_hole()
	{
		_listener.Dispose();
		var logger = new TestLogger<Service>(LogLevel.Information);
		var evaluated = false;

		using (var trace = logger.Trace($"{Evaluate()}"))
		{
			trace.LogDebug($"{Evaluate()}");
		}

		evaluated.ShouldBeFalse();
		logger.Entries.ShouldBeEmpty();

		string Evaluate()
		{
			evaluated = true;
			return "x";
		}
	}

	[Fact]
	public void Scope_is_a_drop_in_logger()
	{
		var logger = new TestLogger<Service>(LogLevel.Information);

		using (var trace = logger.Trace())
		{
			trace.LogInformation("Total {Total}", 3);
			trace.LogWarning($"grid {"main"} is empty");
		}

		logger.Entries.Select(e => e.Message).ShouldBe(["Total 3", "grid main is empty"]);
	}
}
