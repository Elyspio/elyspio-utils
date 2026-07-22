# Elyspio.Utils.Telemetry

`Elyspio.Utils.Telemetry` bootstraps OpenTelemetry tracing and metrics for ASP.NET Core applications targeting .NET 10.

## Installation

```bash
dotnet add package Elyspio.Utils.Telemetry
dotnet add package Elyspio.Utils.Telemetry.MongoDB
dotnet add package Elyspio.Utils.Telemetry.Sql
dotnet add package Elyspio.Utils.Telemetry.Redis
```

## Quick start

```csharp
using Elyspio.Utils.Telemetry.Technical.Extensions;
using Elyspio.Utils.Telemetry.Tracing.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilogWithTelemetry();

if (builder.Configuration.IsTelemetryEnabled(out var options))
{
    var telemetry = new AppOpenTelemetryBuilder<Program>(options!, builder.Configuration);
    telemetry.Build(builder.Services);
    builder.Services.AddOpenTelemetryJsonConfiguration(builder.Configuration);
}

var app = builder.Build();
app.UseOpenTelemetryJsonConfiguration();
```

`AddOpenTelemetryJsonConfiguration` reads `OpenTelemetry:Capture` by default. `UseOpenTelemetryJsonConfiguration` applies it immediately and refreshes the capture cache when configuration reloads.

```json
{
  "OpenTelemetry": {
    "CollectorUri": "http://localhost:4318/",
    "Service": "my-service",
    "Protocol": "HttpProtobuf",
    "Capture": {
      "Activated": true,
      "Levels": { "Debug": false, "Trace": false }
    }
  }
}
```

MongoDB instrumentation protects GridFS chunk commands by recording `GridFS` instead of the command payload. SQL instrumentation omits Hangfire commands and, when `Capture:Levels:Debug` is enabled, adds parameter tags.
