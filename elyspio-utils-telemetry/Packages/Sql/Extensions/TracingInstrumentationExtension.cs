using System.Diagnostics;
using System.Text.Json;
using Elyspio.Utils.Telemetry.Sql.Helpers;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Microsoft.Data.SqlClient;
using OpenTelemetry.Instrumentation.SqlClient;
using OpenTelemetry.Trace;

namespace Elyspio.Utils.Telemetry.Sql.Extensions;

/// <summary>Extensions for SQL Client instrumentation.</summary>
public static class TracingInstrumentationExtension
{
	/// <summary>Adds SQL Client tracing instrumentation with application-specific enrichment.</summary>
	/// <param name="builder">The tracer provider builder to configure.</param>
	/// <param name="action">An optional callback for additional SQL Client instrumentation configuration.</param>
	/// <returns>The configured tracer provider builder.</returns>
	public static TracerProviderBuilder AddAppSqlClientInstrumentation(this TracerProviderBuilder builder, Action<SqlClientTraceInstrumentationOptions>? action = null)
	{
		builder.AddSqlClientInstrumentation(options =>
		{
			options.RecordException = true;
			options.EnrichWithSqlCommand = (activity, value) =>
			{
				if (value is not SqlCommand command || !TracingContext.OpenTelemetryOptions.Cache.Activated) return;
				var tables = SqlHelper.ExtractTablesFromQuery(command.CommandText); tables.Sort(StringComparer.OrdinalIgnoreCase);
				var parts = new[] { command.Connection?.Database, SqlHelper.ExtractCommandFromQuery(command.CommandText).ToUpperInvariant(), string.Join(", ", tables) }
					.Where(part => !string.IsNullOrWhiteSpace(part));
				activity.DisplayName = string.Join(" - ", parts);
				if (!TracingContext.OpenTelemetryOptions.Cache.Levels.TryGetValue(CaptureLevel.Debug, out var debug) || !debug) return;
				var parameters = SqlHelper.ExtractParameterValues(command.Parameters);
				if (parameters.Count == 0) return;
				activity.SetTag("db.query.parameters", JsonSerializer.Serialize(parameters));
				foreach (var parameter in parameters) activity.SetTag($"db.query.parameter.{parameter.Key}", parameter.Value);
			};
			var filter = options.Filter;
			options.Filter = value => IsNotHangfireCommand(value) && (filter?.Invoke(value) ?? true);
			action?.Invoke(options);
		});
		return builder;
	}

	private static bool IsNotHangfireCommand(object command) => command is not SqlCommand sql ||
		!sql.CommandText.Contains("Hangfire.", StringComparison.OrdinalIgnoreCase) &&
		!sql.CommandText.Contains("[Hangfire].", StringComparison.OrdinalIgnoreCase);
}
