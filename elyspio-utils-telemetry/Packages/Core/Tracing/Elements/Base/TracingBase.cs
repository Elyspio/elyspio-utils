using System.Diagnostics;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Microsoft.Extensions.Logging;

namespace Elyspio.Utils.Telemetry.Tracing.Elements.Base;

/// <summary>
///     Tracing context for Services and Adapters
/// </summary>
public abstract class TracingBase
{
    /// <summary>
    ///     ILogger instance from DI
    /// </summary>
    protected readonly ILogger _logger;

	private readonly string _sourceName;


    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="logger"></param>
    protected TracingBase(ILogger logger)
	{
		_logger = logger;
		_sourceName = GetType().Name;
		TracingContext.AddSource(_sourceName);
	}


    /// <summary>
    ///     Get the current <see cref="ActivitySource" />
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
	protected ActivitySource ActivitySource => TracingContext.GetActivitySource(_sourceName);


    /// <summary>
    ///     Create a logger instance for a specific call
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="level"></param>
    /// <param name="method">name of the method (auto)</param>
    /// <param name="fullFilePath">filename of the method (auto)</param>
    /// <param name="autoExit">Pass false to handle <see cref="Log.LoggerInstance.Exit" /> yourself</param>
    /// <param name="logOnExit">if the logger will log in Serilog on exit</param>
    /// <param name="createActivity">If an activity should be created</param>
    /// <returns></returns>
    internal Log.LoggerInstance LogInternal(string arguments, LogLevel level, string method, string fullFilePath, bool autoExit, bool logOnExit, bool createActivity)
	{
		method = TracingContext.GetMethodName(method);

		var className = Log.GetClassNameFromFilepath(fullFilePath);

		var activity = createActivity ? ActivitySource.CreateActivity(className, method, arguments) : null;

		return _logger.Enter(arguments, level, activity, method, autoExit, className, logOnExit);
	}
}