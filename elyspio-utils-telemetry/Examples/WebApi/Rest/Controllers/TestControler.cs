using Elyspio.Utils.Telemetry.Technical.Options.Cache;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Microsoft.AspNetCore.Mvc;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Controllers;

[ApiController]
[Route("tests")]
public class TestControler(ILogger<TestControler> logger, IServiceProvider serviceProvider, IConfiguration configuration) : TracingController(logger)
{
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private IConfiguration _configuration = configuration;

	[HttpGet("telemetry/options")]
	[ProducesResponseType(typeof(TelemetryCaptureCache), StatusCodes.Status200OK)]
	public IActionResult GetTelemetryOptions()
	{
		using var _ = LogController();

		return Ok(TracingContext.OpenTelemetryOptions.CaptureCache);
	}
}