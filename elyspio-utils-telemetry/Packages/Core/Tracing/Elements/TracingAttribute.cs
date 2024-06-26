using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Elyspio.Utils.Telemetry.Tracing.Markers;
using Microsoft.Extensions.Logging;

namespace Elyspio.Utils.Telemetry.Tracing.Elements;

/// <inheritdoc cref="Attribute" />
/// Add tracing context, used for filters
public abstract class TracingAttribute : Attribute, ITracingContext, ITracingAttribute
{
	private readonly string _sourceName;

	/// <inheritdoc cref="TracingAttribute" />
	protected TracingAttribute()
	{
		_sourceName = GetType().Name;
		TracingContext.AddSource(_sourceName);
	}

    /// <summary>
    ///     A logger
    /// </summary>
    public abstract ILogger Logger { get; set; }

	private ActivitySource ActivitySource => TracingContext.GetActivitySource(_sourceName);


    /// <summary>
    ///     Start a new Activity for this context
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="method"></param>
    /// <param name="fullFilePath"></param>
    /// <param name="autoExit"></param>
    /// <returns></returns>
    protected Log.LoggerInstance LogAttribute(string arguments = "", [CallerMemberName] string method = "", [CallerFilePath] string fullFilePath = "", bool autoExit = true)
	{
		method = TracingContext.GetMethodName(method);

		var className = Log.GetClassNameFromFilepath(fullFilePath);
		var activity = TracingContext.OpenTelemetryOptions.CaptureCache.Components[CaptureComponent.Attribute] ? ActivitySource.CreateActivity(className, method, arguments) : null;
		return Logger.Enter(arguments, LogLevel.Debug, activity, method, autoExit, className);
	}
}