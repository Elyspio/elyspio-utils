using Elyspio.Utils.Results.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Xunit;

namespace Elyspio.Utils.Results.Tests;

public sealed class ResultHttpExtensionsTests
{
	private sealed record ResourceIdInvalid(string ResourceId) : Invalid("ores.invalid-resource-id", "Invalid resource id.")
	{
		public override IEnumerable<KeyValuePair<string, object?>> Extensions => [new("resourceId", ResourceId)];
	}

	public static TheoryData<Error, int> Categories => new()
	{
		{ new Invalid("c", "m"), 400 },
		{ new Unauthorized("c", "m"), 401 },
		{ new Forbidden("c", "m"), 403 },
		{ new NotFound("c", "m"), 404 },
		{ new Conflict("c", "m"), 409 },
		{ new TooManyRequests("c", "m"), 429 },
		{ new Unexpected("c", "m"), 500 },
		{ new BadGateway("c", "m"), 502 },
		{ new Unavailable("c", "m"), 503 },
		{ new Timeout("c", "m"), 504 },
		{ new Failure(418, "c", "m"), 418 },
	};

	[Theory]
	[MemberData(nameof(Categories))]
	public void Category_gives_the_status(Error error, int status)
	{
		var problem = error.ToProblem().ShouldBeOfType<ProblemHttpResult>();

		problem.StatusCode.ShouldBe(status);
		problem.ProblemDetails.Type.ShouldBe("urn:problem:c");
		problem.ProblemDetails.Extensions["code"].ShouldBe("c");
	}

	[Fact]
	public void Unexpected_hides_its_message()
	{
		var problem = new Unexpected("bug", "NullReferenceException in Foo").ToProblem().ShouldBeOfType<ProblemHttpResult>();

		problem.ProblemDetails.Detail.ShouldNotContain("NullReference");
	}

	[Fact]
	public void Extensions_are_published()
	{
		var problem = new ResourceIdInvalid("x!").ToProblem().ShouldBeOfType<ProblemHttpResult>();

		problem.ProblemDetails.Extensions["resourceId"].ShouldBe("x!");
	}

	[Fact]
	public void Invalid_with_fields_gives_a_validation_problem()
	{
		var error = new Invalid("form.invalid", "Invalid form.") { Fields = new Dictionary<string, string[]> { ["name"] = ["Required."] } };

		var problem = error.ToProblem().ShouldBeOfType<ValidationProblem>();

		problem.StatusCode.ShouldBe(400);
		problem.ProblemDetails.Errors["name"].ShouldBe(["Required."]);
	}

	[Fact]
	public void Success_gives_200_204_or_the_custom_result()
	{
		Result<int> value = 1;
		Result<Unit> unit = Unit.Value;

		value.ToHttpResult().ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<int>>();
		unit.ToHttpResult().ShouldBeOfType<NoContent>();
		value.ToHttpResult(v => TypedResults.Accepted($"/runs/{v}")).ShouldBeOfType<Accepted>();
	}
}
