# Elyspio.Utils.Telemetry

OpenTelemetry for ASP.NET Core apps on .NET 11, configured only through the standard `OTEL_*` settings, plus composition-based trace scopes:

```csharp
using var trace = logger.Trace($"{Log.F(grid)}");
```

## Installation

```bash
dotnet add package Elyspio.Utils.Telemetry
dotnet add package Elyspio.Utils.Telemetry.MongoDB   # optional
dotnet add package Elyspio.Utils.Telemetry.Sql       # optional
dotnet add package Elyspio.Utils.Telemetry.Redis     # optional
```

`Elyspio.Utils.Telemetry` depends on [`Elyspio.Utils.Results`](../elyspio-utils-results/readme.md) and ships the `ELY0001` analyzer.

## Bootstrap

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddElyspioTelemetry(); // or: o => o.Sources.Add("Yarp.ReverseProxy")
```

- **Logs**: Serilog, always. Console output, levels from the `Serilog` section of the configuration. Logs are exported to OTLP through the Serilog sink when an endpoint is set.
- **Traces and metrics**: exported only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Covers ASP.NET Core, HttpClient, the `System.Runtime` meter and the app's own `ActivitySource`s (the entry assembly root, `GtnhApp` + `GtnhApp.*`).
- **Configuration**: the standard keys, read from `IConfiguration` (environment variables or appsettings): `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, and so on. Only the generic `OTEL_EXPORTER_OTLP_*` keys are supported: the per-signal ones (`OTEL_EXPORTER_OTLP_TRACES_*`, `_METRICS_*`, `_LOGS_*`) are not applied. One host per process: the options (`MirrorLogsToSpanEvents`, `LogFields`) are process-wide. Aspire sets them for you; in Kubernetes the chart sets them:

```yaml
env:
  - name: OTEL_EXPORTER_OTLP_ENDPOINT
    value: http://collector-telemetry-service.telemetry.svc.cluster.local:4318
  - name: OTEL_EXPORTER_OTLP_PROTOCOL
    value: http/protobuf
  - name: OTEL_SERVICE_NAME
    value: gtnh-app
```

- **Probes**: `/health`, `/healthz`, `/healthcheck`, `/alive` and `/metrics` (with their sub-paths) are never traced. Nothing they call is traced either, so a readiness check that queries the database leaves no orphan span. Configure this with `UntracedPaths`.
- **Response headers**: every response with a recorded trace carries `traceresponse: 00-{trace-id}-{span-id}-01` and `X-Trace-Id: {trace-id}`. A cross-origin SPA reads them only when CORS exposes them: `policy.WithExposedHeaders(TraceHeaders.All)`. Turn this off with `TraceResponseHeaders = false`.

Options (`ElyspioTelemetryOptions`): `Sources`, `UntracedPaths`, `TraceResponseHeaders`, `MirrorLogsToSpanEvents` (copy scope logs as span events; off by default because logs already carry `trace_id`/`span_id`), `ConsoleTemplate`, `LogFields` (Log.F settings), and `Tracing`/`Metrics` (callbacks for extra instrumentation):

```csharp
builder.AddElyspioTelemetry(o => o.Tracing = tracing => tracing.AddAppMongoInstrumentation());
```

## Trace scopes

```csharp
public sealed class OresService(BridgeAdapter bridge, ILogger<OresService> logger)
{
	public async Task<Result<Stock>> GetStock(string grid, CancellationToken cancellationToken)
	{
		using var trace = logger.Trace($"{Log.F(grid)}");
		trace.Activity?.SetTag("ores.grid", grid);

		var read = await bridge.ReadMetrics(grid, cancellationToken);
		if (!read.TryGet(out var amounts, out var error)) return trace.Fail(error);

		if (amounts.Count == 0) trace.LogWarning($"grid {grid} is empty");

		trace.Exit($"{amounts.Count} entries");
		return new Stock(grid, amounts);
	}
}
```

- `logger.Trace(...)` opens a span named `{Class}.{Method}` and logs `Enter` with the arguments. Disposing the scope logs `Exit` with the elapsed time. Both are logged at Debug by default; pass a level with `logger.Trace(LogLevel.Information, $"...")`.
- **Allocations**: interpolated string handlers are used throughout. When the level is disabled and no listener records the span, the holes are not evaluated and nothing is allocated.
- **`TraceScope` is an `ILogger`**: `LogXxx` extensions and `[LoggerMessage]` methods work on it, so it is a drop-in replacement for the class logger.
- **Failure**: `return trace.Fail(error);` marks the span (status `Error`, `error.type` = error code) and the `Exit` log. `return trace.Return(result);` does the same for a `Result` relayed as is. An exception logged through the scope (`trace.LogError(ex, $"...")`) is recorded on the span.
- **`trace.Exit($"...")`** sets the content of the `Exit` log and the `code.exit` tag.
- **ELY0001**: a warning when a method that holds a `TraceScope` returns an error without `trace.Fail`. It comes with a code fix. Not checked: an error returned from a `?:` or `switch` expression arm.

### Log.F

`Log.F(value)` writes `name=<json>` directly into the pooled buffer of the handler, and only when the log is enabled:

- Scalars are written as is. Objects are serialized once into a pooled UTF-8 buffer.
- Output is capped at 8 KB (`MaxLength`) and depth 16. Binary data is summarized (`"(1000000 bytes)"`).
- `[LogMask]` writes `***`. `[LogIgnore]` and Microsoft's `[LogPropertyIgnore]` drop the property. For types you do not own, use `LogFields = o => o.Ignore<T>("Password")`.
- These settings use a JSON configuration dedicated to logs: the API's JSON is unaffected.

## Instrumentation packages

| Package | Method | Notes |
| --- | --- | --- |
| `Elyspio.Utils.Telemetry.MongoDB` | `AddAppMongoInstrumentation()` + `MongoDbActivityEventSubscriber` | GridFS chunk payloads replaced by `GridFS`. |
| `Elyspio.Utils.Telemetry.Sql` | `AddAppSqlClientInstrumentation(captureParameters: false)` | Spans named `database - COMMAND - tables`, Hangfire polling ignored. Parameter values are recorded only on opt-in. |
| `Elyspio.Utils.Telemetry.Redis` | `AddAppRedisInstrumentation()` | Spans named `Redis (COMMAND)`. |

## Migrating from 2.x

| 2.x | 3.0 |
| --- | --- |
| `OpenTelemetry:CollectorUri`, `Service`, `Protocol` | `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`, `OTEL_EXPORTER_OTLP_PROTOCOL` |
| `builder.Host.UseSerilogWithTelemetry()` + `IsTelemetryEnabled` + `AppOpenTelemetryBuilder<Program>` | `builder.AddElyspioTelemetry()` |
| `AddOpenTelemetryJsonConfiguration` / `UseOpenTelemetryJsonConfiguration`, `OpenTelemetry:Capture` | Removed: Serilog levels drive the Enter/Exit logs, the trace sampler drives the spans |
| `TracingController`, `TracingService`, `TracingAdapter`, `TracingRepository` base classes | No base class: `using var trace = logger.Trace(...)` in the method |
| `using var logger = LogService(...)`, `logger.Success(...)` | `trace.Exit($"...")` |
| Business exceptions caught by a filter | `Result<T>` + `trace.Fail(error)`; [`ToHttpResult()`](../elyspio-utils-results/readme.md) at the endpoint |
| `Log.F(value)` returning a string | `Log.F(value)` returning a `LogField<T>`, formatted only when the log is enabled |
| mTLS client certificate options | Removed: use `OTEL_EXPORTER_OTLP_CERTIFICATE` / `OTEL_EXPORTER_OTLP_CLIENT_*` |
| SQL parameters captured when `Capture:Levels:Debug` | `AddAppSqlClientInstrumentation(captureParameters: true)` |

Controllers that only passed calls through no longer need their own span: the ASP.NET Core server span covers them.

## Example

`Examples/AppHost` runs `Examples/WebApi` under Aspire. The dashboard shows its traces, metrics and logs:

```bash
aspire run
```
