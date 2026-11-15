using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace Elyspio.Utils.Telemetry.Tests;

public sealed class TelemetryExtensionsTests
{
	private static async Task<WebApplication> Start(string? endpoint)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		if (endpoint is not null) builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint;
		builder.AddElyspioTelemetry();

		var app = builder.Build();
		app.MapGet("/api/ping", () => "pong");
		app.MapGet("/health", () => "ok");

		await app.StartAsync(TestContext.Current.CancellationToken);
		return app;
	}

	[Fact]
	public async Task Recorded_requests_carry_the_trace_id()
	{
		await using var app = await Start("http://localhost:4318");

		var response = await app.GetTestClient().GetAsync("/api/ping", TestContext.Current.CancellationToken);

		var traceId = response.Headers.GetValues(TraceHeaders.TraceId).Single();
		traceId.Length.ShouldBe(32);
		response.Headers.GetValues(TraceHeaders.TraceResponse).Single().ShouldStartWith($"00-{traceId}-");
	}

	[Fact]
	public async Task Probes_are_not_traced()
	{
		await using var app = await Start("http://localhost:4318");

		var response = await app.GetTestClient().GetAsync("/health", TestContext.Current.CancellationToken);

		response.Headers.Contains(TraceHeaders.TraceId).ShouldBeFalse();
	}

	[Fact]
	public async Task A_span_left_by_an_exception_is_marked_as_failed()
	{
		await using var app = await Start("http://localhost:4318");
		using var source = new ActivitySource("Elyspio.Tests.Unwinding");
		Activity? activity = null;

		try
		{
			using (activity = source.StartActivity("job")) throw new InvalidOperationException("boom");
		}
		catch (InvalidOperationException)
		{
		}

		activity.ShouldNotBeNull().Status.ShouldBe(ActivityStatusCode.Error);
	}

	[Fact]
	public async Task Without_endpoint_nothing_is_exported()
	{
		await using var app = await Start(null);

		var response = await app.GetTestClient().GetAsync("/api/ping", TestContext.Current.CancellationToken);

		response.Headers.Contains(TraceHeaders.TraceId).ShouldBeFalse();
	}

	[Theory]
	[InlineData("/health", true)]
	[InlineData("/health/live", true)]
	[InlineData("/HEALTHZ", true)]
	[InlineData("/metrics", true)]
	[InlineData("/healthy", false)]
	[InlineData("/api/health", false)]
	public void Untraced_paths_match_whole_segments(string path, bool untraced)
	{
		TelemetryExtensions.IsUntraced(path, [.. new ElyspioTelemetryOptions().UntracedPaths]).ShouldBe(untraced);
	}
}
