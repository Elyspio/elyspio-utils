using Microsoft.AspNetCore.Http;

namespace Elyspio.Utils.Results.AspNetCore;

public static class ResultHttpExtensions
{
	private const string UnexpectedDetail = "Une erreur inattendue est survenue.";

	/// <summary>200 with the value, or the ProblemDetails of the error.</summary>
	public static IResult ToHttpResult<T>(this Result<T> result) => result.ToHttpResult(static value => TypedResults.Ok(value));

	/// <summary>204, or the ProblemDetails of the error.</summary>
	public static IResult ToHttpResult(this Result<Unit> result) => result.ToHttpResult(static _ => TypedResults.NoContent());

	/// <summary>Endpoint-specific success (202 + Location, 201…), or the ProblemDetails of the error.</summary>
	public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) => result switch
	{
		Ok<T> ok => onSuccess(ok.Value),
		Err<Error> err => err.Error.ToProblem(),
	};

	public static IResult ToProblem(this Error error)
	{
		// Exhaustive over the closed categories: no default arm, a new category breaks the build here (CS8509).
		var status = error switch
		{
			Invalid => StatusCodes.Status400BadRequest,
			Unauthorized => StatusCodes.Status401Unauthorized,
			Forbidden => StatusCodes.Status403Forbidden,
			NotFound => StatusCodes.Status404NotFound,
			Conflict => StatusCodes.Status409Conflict,
			TooManyRequests => StatusCodes.Status429TooManyRequests,
			Unexpected => StatusCodes.Status500InternalServerError,
			BadGateway => StatusCodes.Status502BadGateway,
			Unavailable => StatusCodes.Status503ServiceUnavailable,
			Timeout => StatusCodes.Status504GatewayTimeout,
			Failure failure => failure.Status,
		};

		// traceId is added by the ASP.NET ProblemDetails service (AddProblemDetails), not here.
		var extensions = new Dictionary<string, object?>(error.Extensions) { ["code"] = error.Code };

		var type = $"urn:problem:{error.Code}";
		var detail = error is Unexpected ? UnexpectedDetail : error.Message;

		if (error is Invalid { Fields: { } fields })
		{
			return TypedResults.ValidationProblem(fields, detail, type: type, extensions: extensions);
		}

		return TypedResults.Problem(detail, statusCode: status, type: type, extensions: extensions);
	}
}
