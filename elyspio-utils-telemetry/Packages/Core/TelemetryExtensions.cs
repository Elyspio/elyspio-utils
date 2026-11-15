using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Elyspio.Utils.Telemetry;

public sealed class ElyspioTelemetryOptions
{
	/// <summary>ActivitySource names or wildcards to listen to. Default: root of the entry assembly, e.g. <c>GtnhApp</c> + <c>GtnhApp.*</c>.</summary>
	public List<string> Sources { get; } = [];

	/// <summary>
	///     Incoming requests never traced, and whose whole processing (HttpClient calls, trace scopes) is not traced
	///     either: probes and Prometheus scrapes. Segment prefixes: <c>/health</c> covers <c>/health/live</c>.
	/// </summary>
	public List<PathString> UntracedPaths { get; } = ["/health", "/healthz", "/healthcheck", "/alive", "/metrics"];

	/// <summary>Send <c>traceresponse</c> and <c>X-Trace-Id</c> on every response whose trace is recorded.</summary>
	public bool TraceResponseHeaders { get; set; } = true;

	/// <summary>Also copy every log written through a TraceScope as an event on its span (off: logs already carry trace_id/span_id).</summary>
	public bool MirrorLogsToSpanEvents { get; set; }

	/// <summary>Console output template of Serilog.</summary>
	public string ConsoleTemplate { get; set; } = "{Level:u3} {SourceContext}: {Message:lj}{NewLine}{Exception}";

	/// <summary>Log.F settings: max length, ignored properties of types you do not own, AOT JSON contexts, encoder.</summary>
	public Action<LogFieldOptions>? LogFields { get; set; }

	public Action<TracerProviderBuilder>? Tracing { get; set; }
	public Action<MeterProviderBuilder>? Metrics { get; set; }
}

/// <summary>Response headers carrying the trace id. A cross-origin SPA reads them only if CORS exposes them: <c>policy.WithExposedHeaders(TraceHeaders.All)</c>.</summary>
public static class TraceHeaders
{
	/// <summary>W3C Trace Context Level 2 draft: <c>00-{trace-id}-{span-id}-01</c>.</summary>
	public const string TraceResponse = "traceresponse";

	/// <summary>The trace id alone, for humans (toasts, bug reports, Jaeger search).</summary>
	public const string TraceId = "X-Trace-Id";

	public static readonly string[] All = [TraceResponse, TraceId];
}

internal static class TelemetrySettings
{
	public static bool MirrorLogsToSpanEvents { get; set; }
}

public static class TelemetryExtensions
{
	/// <summary>
	///     Serilog on the console (levels from the <c>Serilog</c> section) in every case. When OTEL_EXPORTER_OTLP_ENDPOINT
	///     is set (Aspire, K8s chart, appsettings): traces and metrics through the OTel SDK, logs through the Serilog OTLP
	///     sink, all configured by the standard OTEL_* keys read from IConfiguration (environment or appsettings).
	/// </summary>
	public static IHostApplicationBuilder AddElyspioTelemetry(this IHostApplicationBuilder builder, Action<ElyspioTelemetryOptions>? configure = null)
	{
		var options = new ElyspioTelemetryOptions();
		var root = System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!.Split('.')[0];
		options.Sources.AddRange([root, $"{root}.*"]);
		configure?.Invoke(options);
		TelemetrySettings.MirrorLogsToSpanEvents = options.MirrorLogsToSpanEvents;
		if (options.LogFields is { } logFields) LogFieldOptions.Configure(logFields);

		var configuration = builder.Configuration;
		var exportEnabled = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

		// One log pipeline, one OTLP log exporter: Serilog (the SDK exports traces and metrics only).
		builder.Services.AddSerilog((services, logger) =>
		{
			logger.ReadFrom.Configuration(configuration)
				.Enrich.FromLogContext()
				.WriteTo.Console(outputTemplate: options.ConsoleTemplate, formatProvider: System.Globalization.CultureInfo.InvariantCulture);

			// Same keys as the SDK (endpoint, protocol, headers, service name, resource attributes), from IConfiguration.
			if (exportEnabled) logger.WriteTo.OpenTelemetry(_ => { }, key => configuration[key]);
		});

		if (!exportEnabled) return builder;

		PathString[] untraced = [.. options.UntracedPaths];
		builder.Services.AddTransient<IStartupFilter>(_ => new TelemetryStartupFilter(untraced, options.TraceResponseHeaders));

		builder.Services.AddOpenTelemetry()
			.WithTracing(tracing =>
			{
				tracing.AddAspNetCoreInstrumentation(o => o.Filter = context => !IsUntraced(context.Request.Path, untraced))
					.AddHttpClientInstrumentation()
					.AddSource([.. options.Sources])
					.AddOtlpExporter();
				options.Tracing?.Invoke(tracing);
			})
			.WithMetrics(metrics =>
			{
				metrics.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddMeter("System.Runtime")
					.AddOtlpExporter();
				options.Metrics?.Invoke(metrics);
			});

		return builder;
	}

	internal static bool IsUntraced(PathString path, PathString[] untraced)
	{
		foreach (var prefix in untraced)
		{
			if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)) return true;
		}

		return false;
	}

	/// <summary>
	///     First middleware of the pipeline: suppresses instrumentation for the whole processing of an untraced request
	///     (a readiness check calling a dependency creates no orphan span), and puts the trace id on the response.
	/// </summary>
	private sealed class TelemetryStartupFilter(PathString[] untraced, bool traceResponseHeaders) : IStartupFilter
	{
		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
		{
			app.Use(async (context, nextMiddleware) =>
			{
				if (IsUntraced(context.Request.Path, untraced))
				{
					using var _ = SuppressInstrumentationScope.Begin();
					await nextMiddleware(context);
					return;
				}

				if (traceResponseHeaders) context.Response.OnStarting(AddTraceHeaders, context);
				await nextMiddleware(context);
			});
			next(app);
		};

		private static Task AddTraceHeaders(object state)
		{
			var context = (HttpContext)state;

			// Only a recorded trace: an id missing from Jaeger would mislead.
			if (context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpActivityFeature>()?.Activity is { Recorded: true } activity)
			{
				var traceId = activity.TraceId.ToHexString();
				context.Response.Headers[TraceHeaders.TraceResponse] = $"00-{traceId}-{activity.SpanId.ToHexString()}-01";
				context.Response.Headers[TraceHeaders.TraceId] = traceId;
			}

			return Task.CompletedTask;
		}
	}
}
