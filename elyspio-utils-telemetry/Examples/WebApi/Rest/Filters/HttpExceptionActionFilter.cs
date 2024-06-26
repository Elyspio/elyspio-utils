using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Filters;

/// <summary>
///     Filtre sur les exceptions pour les ajouter dans le contexte de trace
/// </summary>
public class HttpExceptionActionFilter : ExceptionFilterAttribute
{
	private readonly ILogger<HttpExceptionActionFilter> _logger;

	public HttpExceptionActionFilter(ILogger<HttpExceptionActionFilter> logger)
	{
		_logger = logger;
	}

	public override void OnException(ExceptionContext context)
	{
		_logger.LogError(context.Exception, "Une erreur est survenue");

		var activity = context.HttpContext.Features[typeof(IHttpActivityFeature)] as IHttpActivityFeature;
		activity!.Activity.SetTag("exception", context.Exception);

		// Retour de l'objet en JSON
		context.Result = new ObjectResult(context.Exception) { StatusCode = 500 };
		//On poursuit le process
		base.OnException(context);
	}
}