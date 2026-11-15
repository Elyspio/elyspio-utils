using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elyspio.Utils.Results;

namespace Elyspio.Utils.Telemetry;

/// <summary>
///     One span + Enter/Exit logs (with the elapsed time) for a method call:
///     <c>using var trace = logger.Trace($"{Log.F(id)}");</c> … <c>return trace.Return(result);</c>
///     <para>
///         Also an <see cref="ILogger" />: every <c>LogXxx</c> extension and every <c>[LoggerMessage]</c> method works on it
///         (drop-in for the class logger). One scope per operation: do not share it across parallel work.
///     </para>
/// </summary>
public sealed class TraceScope : ILogger, IDisposable
{
	private readonly ILogger _logger;
	private readonly string _name;
	private readonly long _startedAt = Stopwatch.GetTimestamp();
	private string? _exitContent;
	private string? _failure;
	private bool _disposed;

	internal TraceScope(ILogger logger, ActivitySource source, string name, string? arguments, LogLevel level)
	{
		_logger = logger;
		_name = name;
		Level = level;

		// Null when nobody listens or instrumentation is suppressed (probes): every span-side member is then a no-op.
		Activity = source.StartActivity(name);
		if (Recording)
		{
			Activity!.SetTag("code.function.name", name);
			if (arguments is not null) Activity.SetTag("code.arguments", arguments);
		}

		TraceLog.Enter(_logger, level, name, arguments);
	}

	/// <summary>The span, for domain tags: <c>trace.Activity?.SetTag("ores.grid", grid)</c>.</summary>
	public Activity? Activity { get; }

	/// <summary>Level of the Enter/Exit logs (Debug by default).</summary>
	public LogLevel Level { get; }

	internal bool Recording => Activity is { IsAllDataRequested: true };

	// ---- Result: the only way a scope is marked as failed ----

	/// <summary>
	///     Marks the span as failed and returns the error, converted by the compiler to the method's result type:
	///     <c>return trace.Fail(new InvalidResourceId(id));</c>
	/// </summary>
	/// <remarks>Preferred over the generic overload, which would otherwise win for any Error subtype (exact type match).</remarks>
	[OverloadResolutionPriority(1)]
	public Failed<Error> Fail(Error error)
	{
		Mark(error.Code);
		return new Failed<Error>(error);
	}

	/// <inheritdoc cref="Fail(Error)" />
	public Failed<TError> Fail<TError>(TError error) where TError : notnull
	{
		Mark(error is Error known ? known.Code : typeof(TError).Name);
		return new Failed<TError>(error);
	}

	/// <summary>Returns the result unchanged; a failure marks the span (status Error, <c>error.type</c>) and the Exit log.</summary>
	public Result<T> Return<T>(Result<T> result)
	{
		if (result.TryGetValue(out Err<Error> err)) Mark(err.Error.Code);
		return result;
	}

	/// <inheritdoc cref="Return{T}(Result{T})" />
	public Result<T, TError> Return<T, TError>(Result<T, TError> result) where TError : notnull
	{
		if (result.TryGetValue(out Err<TError> err)) Mark(err.Error is Error error ? error.Code : typeof(TError).Name);
		return result;
	}

	private void Mark(string errorType)
	{
		_failure = errorType;
		if (!Recording) return;
		Activity!.SetStatus(ActivityStatusCode.Error, errorType);
		Activity.SetTag("error.type", errorType);
	}

	// ---- Interpolated overloads: when the level is disabled, holes are not evaluated and nothing is allocated ----

	public void LogTrace([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<TraceLevel> message) => Write(LogLevel.Trace, null, ref message);

	public void LogDebug([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<DebugLevel> message) => Write(LogLevel.Debug, null, ref message);

	public void LogInformation([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<InformationLevel> message) => Write(LogLevel.Information, null, ref message);

	public void LogWarning([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<WarningLevel> message) => Write(LogLevel.Warning, null, ref message);

	public void LogError([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<ErrorLevel> message) => Write(LogLevel.Error, null, ref message);

	public void LogError(Exception? exception, [InterpolatedStringHandlerArgument("")] ref TraceLogHandler<ErrorLevel> message) => Write(LogLevel.Error, exception, ref message);

	public void LogCritical([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<CriticalLevel> message) => Write(LogLevel.Critical, null, ref message);

	public void LogCritical(Exception? exception, [InterpolatedStringHandlerArgument("")] ref TraceLogHandler<CriticalLevel> message) => Write(LogLevel.Critical, exception, ref message);

	/// <summary>
	///     Content of the Exit log and of the span tag <c>code.exit</c>, e.g. a result summary. Formatted only when the
	///     Exit log is enabled or the span records.
	/// </summary>
	public void Exit([InterpolatedStringHandlerArgument("")] ref TraceLogHandler<ScopeLevel> content)
	{
		if (content.Enabled) _exitContent = content.ToStringAndClear();
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		try
		{
			if (_exitContent is not null && Recording) Activity!.SetTag("code.exit", _exitContent);
			TraceLog.Exit(_logger, Level, _name, _exitContent, _failure, Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds);
		}
		finally
		{
			Activity?.Dispose();
		}
	}

	// ---- ILogger: structured templates and [LoggerMessage] go through here ----

	public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (!_logger.IsEnabled(logLevel)) return;

		_logger.Log(logLevel, eventId, state, exception, formatter);

		// The log already carries trace_id/span_id: copying it on the span is opt-in (formats a second time).
		if (TelemetrySettings.MirrorLogsToSpanEvents && Recording) Activity!.AddEvent(LogEvent(logLevel, formatter(state, exception)));
		if (exception is not null) RecordException(exception, logLevel);
	}

	private void Write<TLevel>(LogLevel level, Exception? exception, ref TraceLogHandler<TLevel> message) where TLevel : ITraceLevel
	{
		if (!message.Enabled) return;

		var text = message.ToStringAndClear();
		_logger.Log(level, default, text, exception, static (s, _) => s);

		if (TelemetrySettings.MirrorLogsToSpanEvents && Recording) Activity!.AddEvent(LogEvent(level, text));
		if (exception is not null) RecordException(exception, level);
	}

	/// <summary>
	///     An exception logged through the scope is recorded on the span. Logged as an error, it marks the span as failed;
	///     below (a warning about a handled failure, a retry), the operation may still succeed and the status is left alone.
	/// </summary>
	private void RecordException(Exception exception, LogLevel level)
	{
		if (!Recording) return;
		Activity!.AddException(exception);
		if (level >= LogLevel.Error) Activity.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
	}

	private static ActivityEvent LogEvent(LogLevel level, string message) => new(message, tags: new() { ["log.severity"] = level.ToString() });
}

public static class TraceExtensions
{
	/// <summary>Opens a <see cref="TraceScope" /> named <c>{T}.{caller}</c>, Enter/Exit at Debug.</summary>
	public static TraceScope Trace<T>(this ILogger<T> logger, [InterpolatedStringHandlerArgument("logger")] ref TraceArgumentsHandler<T> arguments,
		[CallerMemberName] string method = "")
	{
		return new TraceScope(logger, TraceSources.Of<T>(), TraceNames<T>.Get(method), arguments.ToStringOrNull(), LogLevel.Debug);
	}

	/// <summary>Same, with the Enter/Exit level given.</summary>
	public static TraceScope Trace<T>(this ILogger<T> logger, LogLevel level, [InterpolatedStringHandlerArgument("logger", "level")] ref TraceArgumentsHandler<T> arguments,
		[CallerMemberName] string method = "")
	{
		return new TraceScope(logger, TraceSources.Of<T>(), TraceNames<T>.Get(method), arguments.ToStringOrNull(), level);
	}

	/// <summary>No arguments: <c>using var trace = logger.Trace();</c></summary>
	public static TraceScope Trace<T>(this ILogger<T> logger, LogLevel level = LogLevel.Debug, [CallerMemberName] string method = "")
	{
		return new TraceScope(logger, TraceSources.Of<T>(), TraceNames<T>.Get(method), null, level);
	}
}

// ---- Interpolated string handlers: pooled buffer (ArrayPool) ----
// Disabled: holes not evaluated, nothing allocated. Enabled: the final string (Log.F writes straight into the buffer).

/// <summary>
///     Builds the Enter arguments only when the Enter log is enabled or the source has listeners (conservative: a span
///     later dropped by sampling still pays for them; with 100% sampling this never happens).
/// </summary>
[InterpolatedStringHandler]
public ref struct TraceArgumentsHandler<T>
{
	private DefaultInterpolatedStringHandler _inner;
	private readonly bool _enabled;

	public TraceArgumentsHandler(int literalLength, int formattedCount, ILogger<T> logger, out bool enabled)
		: this(literalLength, formattedCount, logger, LogLevel.Debug, out enabled)
	{
	}

	public TraceArgumentsHandler(int literalLength, int formattedCount, ILogger<T> logger, LogLevel level, out bool enabled)
	{
		_enabled = enabled = logger.IsEnabled(level) || TraceSources.Of<T>().HasListeners();
		_inner = enabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
	}

	/// <summary>Log.F: written into the pooled buffer, JSON serialized once, no boxing (more specific than AppendFormatted&lt;TValue&gt;).</summary>
	public void AppendFormatted<TValue>(LogField<TValue> field) => LogFieldWriter.Append(ref _inner, field);

	public void AppendLiteral(string value) => _inner.AppendLiteral(value);
	public void AppendFormatted<TValue>(TValue value) => _inner.AppendFormatted(value);
	public void AppendFormatted<TValue>(TValue value, string? format) => _inner.AppendFormatted(value, format);
	public void AppendFormatted<TValue>(TValue value, int alignment) => _inner.AppendFormatted(value, alignment);
	public void AppendFormatted<TValue>(TValue value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
	public void AppendFormatted(ReadOnlySpan<char> value) => _inner.AppendFormatted(value);
	public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
	public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
	public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
	public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);

	internal string? ToStringOrNull()
	{
		if (!_enabled) return null;
		var text = _inner.ToStringAndClear();
		return text.Length == 0 ? null : text;
	}
}

/// <summary>Builds a message only when <typeparamref name="TLevel" /> is enabled (Exit: or when the span records).</summary>
[InterpolatedStringHandler]
public ref struct TraceLogHandler<TLevel> where TLevel : ITraceLevel
{
	private DefaultInterpolatedStringHandler _inner;

	public TraceLogHandler(int literalLength, int formattedCount, TraceScope trace, out bool enabled)
	{
		Enabled = enabled = TLevel.Level is { } level ? trace.IsEnabled(level) : trace.IsEnabled(trace.Level) || trace.Recording;
		_inner = enabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
	}

	internal bool Enabled { get; }

	/// <summary>Log.F: written into the pooled buffer, JSON serialized once, no boxing (more specific than AppendFormatted&lt;TValue&gt;).</summary>
	public void AppendFormatted<TValue>(LogField<TValue> field) => LogFieldWriter.Append(ref _inner, field);

	public void AppendLiteral(string value) => _inner.AppendLiteral(value);
	public void AppendFormatted<TValue>(TValue value) => _inner.AppendFormatted(value);
	public void AppendFormatted<TValue>(TValue value, string? format) => _inner.AppendFormatted(value, format);
	public void AppendFormatted<TValue>(TValue value, int alignment) => _inner.AppendFormatted(value, alignment);
	public void AppendFormatted<TValue>(TValue value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
	public void AppendFormatted(ReadOnlySpan<char> value) => _inner.AppendFormatted(value);
	public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
	public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
	public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
	public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);

	internal string ToStringAndClear() => _inner.ToStringAndClear();
}

/// <summary>Compile-time level of a <see cref="TraceLogHandler{TLevel}" />; null = the Enter/Exit level of the scope.</summary>
public interface ITraceLevel
{
	static abstract LogLevel? Level { get; }
}

public readonly struct TraceLevel : ITraceLevel { public static LogLevel? Level => LogLevel.Trace; }
public readonly struct DebugLevel : ITraceLevel { public static LogLevel? Level => LogLevel.Debug; }
public readonly struct InformationLevel : ITraceLevel { public static LogLevel? Level => LogLevel.Information; }
public readonly struct WarningLevel : ITraceLevel { public static LogLevel? Level => LogLevel.Warning; }
public readonly struct ErrorLevel : ITraceLevel { public static LogLevel? Level => LogLevel.Error; }
public readonly struct CriticalLevel : ITraceLevel { public static LogLevel? Level => LogLevel.Critical; }
public readonly struct ScopeLevel : ITraceLevel { public static LogLevel? Level => null; }

// ---- Caches: nothing allocated per call once a method has been seen ----

internal static class TraceSources
{
	private static readonly ConcurrentDictionary<Assembly, ActivitySource> Sources = new();

	public static ActivitySource Of<T>() => Cache<T>.Source;

	private static class Cache<T>
	{
		public static readonly ActivitySource Source = Sources.GetOrAdd(typeof(T).Assembly,
			static assembly => new ActivitySource(assembly.GetName().Name!, assembly.GetName().Version?.ToString()));
	}
}

internal static class TraceNames<T>
{
	private static readonly ConcurrentDictionary<string, string> Names = new();

	public static string Get(string method) => Names.GetOrAdd(method, static m => $"{typeof(T).Name}.{m}");
}

/// <summary>Enter/Exit through LoggerMessage.Define: no params array, no boxing.</summary>
internal static class TraceLog
{
	private static readonly Action<ILogger, string, string?, Exception?>[] EnterByLevel = Define<string, string?>("{Method} -- Enter -- {Arguments}");
	private static readonly Action<ILogger, string, double, Exception?>[] ExitByLevel = Define<string, double>("{Method} -- Exit -- {Elapsed}ms");
	private static readonly Action<ILogger, string, string, double, Exception?>[] ExitContentByLevel = Define<string, string, double>("{Method} -- Exit -- {Content} -- {Elapsed}ms");
	private static readonly Action<ILogger, string, string, double, Exception?>[] ExitFailedByLevel = Define<string, string, double>("{Method} -- Exit -- failed: {ErrorType} -- {Elapsed}ms");

	public static void Enter(ILogger logger, LogLevel level, string method, string? arguments)
	{
		if (logger.IsEnabled(level)) EnterByLevel[(int)level](logger, method, arguments, null);
	}

	public static void Exit(ILogger logger, LogLevel level, string method, string? content, string? failure, double elapsed)
	{
		if (!logger.IsEnabled(level)) return;

		if (failure is not null) ExitFailedByLevel[(int)level](logger, method, failure, elapsed, null);
		else if (content is not null) ExitContentByLevel[(int)level](logger, method, content, elapsed, null);
		else ExitByLevel[(int)level](logger, method, elapsed, null);
	}

	private static Action<ILogger, T1, T2, Exception?>[] Define<T1, T2>(string template) =>
		[.. Enumerable.Range(0, (int)LogLevel.None).Select(l => LoggerMessage.Define<T1, T2>((LogLevel)l, default, template, new() { SkipEnabledCheck = true }))];

	private static Action<ILogger, T1, T2, T3, Exception?>[] Define<T1, T2, T3>(string template) =>
		[.. Enumerable.Range(0, (int)LogLevel.None).Select(l => LoggerMessage.Define<T1, T2, T3>((LogLevel)l, default, template, new() { SkipEnabledCheck = true }))];
}
