using Microsoft.Extensions.Logging;

namespace Elyspio.Utils.Telemetry.Tests;

/// <summary>Captures formatted messages at or above the minimum level.</summary>
internal sealed class TestLogger<T>(LogLevel minimumLevel = LogLevel.Trace) : ILogger<T>
{
	public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
	{
		if (IsEnabled(logLevel)) Entries.Add((logLevel, formatter(state, exception), exception));
	}
}
