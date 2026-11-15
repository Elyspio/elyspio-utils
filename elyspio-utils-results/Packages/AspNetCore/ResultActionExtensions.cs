using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elyspio.Utils.Results.AspNetCore;

/// <summary>
///     The MVC counterpart of <see cref="ResultHttpExtensions" />: an action keeps its <c>ActionResult&lt;T&gt;</c> return
///     type, so the OpenAPI document still describes the success body.
/// </summary>
public static class ResultActionExtensions
{
	/// <summary>200 with the value, or the ProblemDetails of the error.</summary>
	public static ActionResult<T> ToActionResult<T>(this Result<T> result)
	{
		return result.TryGet(out var value, out var error) ? value : error.ToActionResult();
	}

	/// <summary>204, or the ProblemDetails of the error.</summary>
	public static ActionResult ToActionResult(this Result<Unit> result)
	{
		return result.TryGet(out _, out var error) ? new NoContentResult() : error.ToActionResult();
	}

	/// <summary>
	///     The ProblemDetails of <see cref="ResultHttpExtensions.ToProblem" /> as an <see cref="ObjectResult" />: executing
	///     the <see cref="IResult" /> from a controller goes through the MVC problem writer, which serializes the declared
	///     <see cref="ProblemDetails" /> type and drops the validation <c>errors</c>.
	/// </summary>
	public static ActionResult ToActionResult(this Error error)
	{
		var problem = (ProblemDetails)((IValueHttpResult)error.ToProblem()).Value!;
		return new ProblemResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
	}

	/// <summary>Adds the <c>traceId</c> the ASP.NET problem details service adds to the other problems.</summary>
	private sealed class ProblemResult(ProblemDetails problem) : ObjectResult(problem)
	{
		public override void OnFormatting(ActionContext context)
		{
			problem.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
			base.OnFormatting(context);
		}
	}
}
