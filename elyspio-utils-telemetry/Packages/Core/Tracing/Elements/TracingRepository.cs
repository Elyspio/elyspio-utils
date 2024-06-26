using System.Runtime.CompilerServices;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Elyspio.Utils.Telemetry.Tracing.Markers;
using Microsoft.Extensions.Logging;

namespace Elyspio.Utils.Telemetry.Tracing.Elements;

/// <summary>
///     Tracing context for Repository
/// </summary>
public abstract class TracingRepository : TracingBase, ITracingRepository
{
	/// <inheritdoc cref="TracingRepository" />
	protected TracingRepository(ILogger logger) : base(logger)
	{
	}

    /// <summary>
    ///     Create a logger instance for a specific call
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="method"></param>
    /// <param name="fullFilePath"></param>
    /// <param name="autoExit"></param>
    /// <param name="logOnExit"></param>
    /// <returns></returns>
    protected Log.LoggerInstance LogRepository(string arguments = "", [CallerMemberName] string method = "", [CallerFilePath] string fullFilePath = "", bool autoExit = true,
		bool logOnExit = true)
	{
		return LogInternal(arguments, LogLevel.Debug, method, fullFilePath, autoExit, logOnExit,
			TracingContext.OpenTelemetryOptions.CaptureCache.Components[CaptureComponent.Repository]);
	}
}