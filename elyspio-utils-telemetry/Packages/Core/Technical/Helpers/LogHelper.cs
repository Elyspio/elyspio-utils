using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Elyspio.Utils.Telemetry.Technical.Helpers;

/// <summary>
///     Provides utility methods for logging.
/// </summary>
public static class Log
{
    /// <summary>
    ///     Custom JSON serializer options with enum as string.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
	{
		Converters =
		{
			new JsonStringEnumConverter()
		}
	};

    /// <summary>
    ///     Formats a named value as a JSON string.
    /// </summary>
    public static string F<T>(T? value, [CallerArgumentExpression("value")] string name = "")
	{
		return $"{name}={Stringify(value)}";
	}

    /// <summary>
    ///     Formats a named value as a JSON string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string Stringify<T>(T? value)
	{
		return JsonSerializer.Serialize(value, Options);
	}

    /// <summary>
    ///     Creates a new instance of the LoggerInstance class for method logging.
    /// </summary>
    public static LoggerInstance Enter(this ILogger logger, string arguments = "", LogLevel level = LogLevel.Debug, Activity? activity = null,
		[CallerMemberName] string method = "", bool autoExit = true, string className = "", bool logOnExit = true, [CallerFilePath] string filepath = "")
	{
		if (string.IsNullOrWhiteSpace(className)) className = GetClassNameFromFilepath(filepath);

		return new LoggerInstance(logger, method, arguments, level, activity, autoExit, className, logOnExit);
	}

    /// <summary>
    ///     Extracts class name from the provided filepath.
    /// </summary>
    public static string GetClassNameFromFilepath(string fullFilePath)
	{
		// On récupère le nom du fichier
		var filePath = fullFilePath[fullFilePath.LastIndexOf(Path.DirectorySeparatorChar)..];

		// On supprime le premier / et l'extension
		return filePath[1..^3];
	}


    /// <summary>
    ///     Encapsulates a logging session, providing methods to record specific events and messages.
    /// </summary>
    public sealed class LoggerInstance : IDisposable
	{
		private readonly string? _arguments;
		private readonly bool _autoExit;
		private readonly string _className;
		private readonly LogLevel _level;
		private readonly ILogger _logger;
		private readonly bool _logOnExit;
		private readonly string _method;
		private readonly long _startedAt = Stopwatch.GetTimestamp();

        /// <summary>
        ///     Creates a new instance of the LoggerInstance class.
        /// </summary>
        /// <param name="logger">The logger to be used.</param>
        /// <param name="method">The method to be logged.</param>
        /// <param name="arguments">The arguments to be logged.</param>
        /// <param name="level">The level of log to be captured.</param>
        /// <param name="activity">Optional activity to correlate logger messages.</param>
        /// <param name="autoExit">True if the log must automatically exit when disposed.</param>
        /// <param name="className">The class name where the logger is used.</param>
        /// <param name="logOnExit">True if the log must automatically exit when disposed.</param>
        public LoggerInstance(ILogger logger, string method, string? arguments, LogLevel level, Activity? activity = null, bool autoExit = true, string className = "",
			bool logOnExit = true)
		{
			_level = level;
			Activity = activity;
			_autoExit = autoExit;
			_method = method;
			_logger = logger;
			_className = className;
			_logOnExit = logOnExit;

			if (!string.IsNullOrWhiteSpace(arguments)) _arguments = arguments;

			Enter();
		}

        /// <summary>
        ///     Current activity
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global - Exported for applications' use
		public Activity? Activity { get; }


        /// <summary>
        ///     Releases associated resources and call <see cref="Exit" /> if <see cref="_autoExit" /> is enabled
        /// </summary>
        public void Dispose()
		{
			if (_autoExit) Exit();
			Activity?.Dispose();
		}


        /// <summary>
        ///     Logs an "entering method" message.
        /// </summary>
        private void Enter()
		{
			if (!_logger.IsEnabled(_level)) return;
			var sb = new StringBuilder($"{_className}.{_method} -- Enter");
			if (!string.IsNullOrWhiteSpace(_arguments))
			{
				sb.Append($" -- {_arguments}");
				if (TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Enter]) Activity?.SetTag("arguments", _arguments);
			}

			var message = sb.ToString();

			_logger.Log(_level, message);
		}


        /// <summary>
        ///     Logs an "exiting method" message.
        /// </summary>
        public void Exit(string? content = null)
		{
			if (!_logger.IsEnabled(_level) || !_logOnExit) return;
			var sb = new StringBuilder($"{_className}.{_method} -- Exit");
			if (_arguments?.Length > 0) sb.Append($" -- {_arguments}");

			if (!string.IsNullOrWhiteSpace(content))
			{
				sb.Append($" -- {content}");

				if (TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Exit]) Activity?.SetTag("log.exit", content);
			}

			sb.Append($" -- {Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds}ms");

			var message = sb.ToString();

			_logger.Log(_level, message);
		}


        /// <summary>
        ///     Logs an error message.
        /// </summary>
        public void Error(string content)
		{
			var sb = new StringBuilder($"{_className}.{_method}");
			if (_arguments?.Length > 0) sb.Append($" -- {_arguments}");
			sb.Append($" -- {content}");

			if (Activity is not null && TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Error]) AddLogToActivity(LogLevel.Error, content);

			_logger.LogError(sb.ToString());
		}

        /// <summary>
        ///     Logs an error message with an exception.
        /// </summary>
        public void Error(Exception e, string content)
		{
			var sb = new StringBuilder($"{_className}.{_method}");
			if (_arguments?.Length > 0) sb.Append($" -- {_arguments}");
			sb.Append($" -- {content}");

			if (TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Error] && Activity is not null) AddLogToActivity(LogLevel.Error, content, e);

			_logger.LogError(e, sb.ToString());
		}

        /// <summary>
        ///     Logs a warning message.
        /// </summary>
        public void Warn(string content)
		{
			var sb = new StringBuilder($"{_className}.{_method} -- {content}");

			if (Activity is not null && TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Warning]) AddLogToActivity(LogLevel.Warning, content);
			;

			_logger.LogWarning(sb.ToString());
		}

        /// <summary>
        ///     Logs a information message.
        /// </summary>
        public void Info(string content)
		{
			var sb = new StringBuilder($"{_className}.{_method} -- {content}");

			if (Activity is not null && TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Information]) AddLogToActivity(LogLevel.Information, content);

			_logger.LogInformation(sb.ToString());
		}


        /// <summary>
        ///     Logs a debug message.
        /// </summary>
        public void Debug(string content)
		{
			if (Activity is not null && TracingContext.OpenTelemetryOptions.CaptureCache.Levels[CaptureLevel.Debug]) AddLogToActivity(LogLevel.Debug, content);

			if (!_logger.IsEnabled(LogLevel.Debug)) return;
			var sb = new StringBuilder($"{_className}.{_method}");
			if (_arguments?.Length > 0) sb.Append($" -- {_arguments}");
			sb.Append($" -- {content}");

			_logger.LogDebug(sb.ToString());
		}


        /// <summary>
        ///     Ajout un log à l'activity courante
        /// </summary>
        /// <param name="level"></param>
        /// <param name="content"></param>
        /// <param name="exception"></param>
        private void AddLogToActivity(LogLevel level, string content, Exception? exception = null)
		{
			if (Activity is null) return;

			var tagLists = new ActivityTagsCollection
			{
				["log.level"] = level.ToString()
			};
			if (exception is not null)
			{
				tagLists.Add("exception.message", exception.Message);
				tagLists.Add("exception.stacktrace", exception.StackTrace);
				tagLists.Add("exception.type", exception.GetType());
			}


			Activity.AddEvent(new ActivityEvent($"{content}", tags: tagLists));
		}
	}
}