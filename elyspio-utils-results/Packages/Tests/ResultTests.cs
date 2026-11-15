using Elyspio.Utils.Results;
using Shouldly;
using Xunit;

namespace Elyspio.Utils.Results.Tests;

public sealed class ResultTests
{
	private sealed record ItemMissing(string Id) : NotFound("items.missing", "Item not found.");

	[Fact]
	public void Value_converts_to_a_success()
	{
		Result<int> result = 42;

		result.IsSuccess.ShouldBeTrue();
		result.TryGet(out var value, out _).ShouldBeTrue();
		value.ShouldBe(42);
	}

	[Fact]
	public void Error_converts_to_a_failure()
	{
		Result<int> result = new ItemMissing("a");

		result.IsFailure.ShouldBeTrue();
		result.TryGet(out _, out var error).ShouldBeFalse();
		error.ShouldBeOfType<ItemMissing>().Id.ShouldBe("a");
	}

	[Fact]
	public void Failed_converts_to_any_result()
	{
		Result<string> result = new Failed(new ItemMissing("a"));

		result.IsFailure.ShouldBeTrue();
	}

	[Fact]
	public void Success_and_failure_never_overlap_when_both_types_are_equal()
	{
		Result<string, string> success = new Ok<string>("value");
		Result<string, string> failure = new Err<string>("error");

		Describe(success).ShouldBe("ok value");
		Describe(failure).ShouldBe("err error");
	}

	[Fact]
	public void Switch_is_exhaustive_over_cases_and_categories()
	{
		Result<int> missing = new ItemMissing("a");

		var description = missing switch
		{
			Ok<int> ok => $"ok {ok.Value}",
			Err<Error> { Error: NotFound } => "not found",
			Err<Error> err => err.Error.Code,
		};

		description.ShouldBe("not found");
	}

	[Fact]
	public void Default_result_is_neither_success_nor_failure()
	{
		Result<int> result = default;

		result.IsSuccess.ShouldBeFalse();
		result.IsFailure.ShouldBeFalse();
		Should.Throw<InvalidOperationException>(() => result.TryGet(out _, out _));
		Should.Throw<InvalidOperationException>(() => result.Map(value => value + 1));
	}

	[Fact]
	public void Map_and_bind_apply_on_success_only()
	{
		Result<int> success = 2;
		Result<int> failure = new ItemMissing("a");

		success.Map(value => value * 10).TryGet(out var mapped, out _).ShouldBeTrue();
		mapped.ShouldBe(20);

		success.Bind<int>(_ => new ItemMissing("b")).TryGet(out _, out var bound).ShouldBeFalse();
		bound.ShouldBeOfType<ItemMissing>().Id.ShouldBe("b");

		var called = false;
		failure.Map(value => called = true).IsFailure.ShouldBeTrue();
		called.ShouldBeFalse();
	}

	private static string Describe(Result<string, string> result) => result switch
	{
		Ok<string> ok => $"ok {ok.Value}",
		Err<string> err => $"err {err.Error}",
	};
}
