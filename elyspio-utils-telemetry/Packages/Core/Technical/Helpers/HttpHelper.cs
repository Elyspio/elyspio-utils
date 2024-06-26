using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Elyspio.Utils.Telemetry.Technical.Helpers;

/// <summary>
///     Http helper
/// </summary>
public static class HttpHelper
{
	/// <summary>
	///     Get the route template from the called endpoint
	/// </summary>
	/// <param name="res"></param>
	/// <returns></returns>
	private static string? GetRouteTemplate(HttpResponse res)
	{
		var endpoint = res.HttpContext.GetEndpoint();

		return endpoint is RouteEndpoint routeEndpoint ? routeEndpoint.RoutePattern.RawText : null;
	}


	/// <summary>
	///     Enrich the activity with the HTTP response (idflux)
	/// </summary>
	/// <param name="activity"></param>
	/// <param name="response"></param>
	public static void EnrichWithHttpResponse(Activity activity, HttpResponse response)
	{
		var template = GetRouteTemplate(response);
		activity.DisplayName = $"HTTP - {response.HttpContext.Request.Method} - {template}";
	}
}