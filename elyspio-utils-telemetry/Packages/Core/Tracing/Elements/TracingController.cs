using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Elyspio.Utils.Telemetry.Tracing.Markers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Elyspio.Utils.Telemetry.Tracing.Elements;

/// <inheritdoc cref="ControllerBase" />
/// with tracing context
public abstract class TracingController : ControllerBase, ITracingContext, ITracingController
{
    /// <summary>
    ///     A logger for this class
    /// </summary>
    protected readonly ILogger _logger;

	private readonly string _sourceName;


	/// <inheritdoc cref="TracingController" />
	protected TracingController(ILogger logger)
	{
		_logger = logger;
		_sourceName = GetType().Name;
		TracingContext.AddSource(_sourceName);
	}

	private ActivitySource ActivitySource => TracingContext.GetActivitySource(_sourceName);


    /// <summary>
    ///     Create a logger instance for a specific call
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="method"></param>
    /// <param name="fullFilePath"></param>
    /// <param name="autoExit"></param>
    /// <returns></returns>
    protected Log.LoggerInstance LogController(string arguments = "", [CallerMemberName] string method = "", [CallerFilePath] string fullFilePath = "", bool autoExit = true)
	{
		method = TracingContext.GetMethodName(method);

		var className = Log.GetClassNameFromFilepath(fullFilePath);
		var activity = TracingContext.OpenTelemetryOptions.CaptureCache.Components[CaptureComponent.Controller]
			? ActivitySource.CreateActivity(className, method, arguments)
			: null;

		return _logger.Enter(arguments, LogLevel.Information, activity, method, autoExit, className);
	}
}