# Elyspio.Utils.Telemetry

`Elyspio.Utils.Telemetry` bootstraps OpenTelemetry tracing and metrics for ASP.NET Core applications.

## Requirements

- .NET 10 (`net10.0`)

## Installation

Core package:

```bash
dotnet add package Elyspio.Utils.Telemetry
```

Optional instrumentation packages:

```bash
dotnet add package Elyspio.Utils.Telemetry.MongoDB
dotnet add package Elyspio.Utils.Telemetry.Sql
dotnet add package Elyspio.Utils.Telemetry.Redis
dotnet add package Elyspio.Utils.Telemetry.MassTransit
```

## Quick start

```csharp
using Coexya.Utils.Telemetry.Tracing.Builder;
using Elyspio.Utils.Telemetry.MassTransit.Extensions;
using Elyspio.Utils.Telemetry.MongoDB.Extensions;
using Elyspio.Utils.Telemetry.Redis.Extensions;
using Elyspio.Utils.Telemetry.Sql.Extensions;
using Elyspio.Utils.Telemetry.Technical.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilogWithTelemetry();

if (builder.Configuration.IsTelemetryEnabled(out var telemetryOptions))
{
	var telemetryBuilder = new AppOpenTelemetryBuilder<Program>(telemetryOptions!, builder.Configuration)
	{
		Tracing = (tracing, _) => tracing
			.AddAppMongoInstrumentation()
			.AddAppSqlClientInstrumentation()
			.AddAppRedisInstrumentation()
			.AddAppMassTransitInstrumentation(),
		Meter = meter => meter.AddAppMassTransitInstrumentation()
	};

	telemetryBuilder.Build(builder.Services);
}
```

## MongoDB tracing

To capture MongoDB commands, subscribe `MongoDbActivityEventSubscriber` when creating the client:

```csharp
var mongoUrl = new MongoUrl(connectionString);
var clientSettings = MongoClientSettings.FromUrl(mongoUrl);

clientSettings.ClusterConfigurator = cb => { cb.Subscribe(new MongoDbActivityEventSubscriber()); };

var client = new MongoClient(clientSettings);
```

## MassTransit consumers

To enrich consumer traces, inherit from `TracingConsumer<TMessage>` and implement `ConsumeAsync`:

```csharp
using Elyspio.Utils.Telemetry.MassTransit.Tracing;
using MassTransit;

public class ToggleTodoConsumer : TracingConsumer<ToggleTodoMessage>
{
	protected override async Task ConsumeAsync(ConsumeContext<ToggleTodoMessage> context)
	{
		// Process message
	}
}
```

## Configuration

Add this section to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "CollectorUri": "http://localhost:4318/",
    "Service": "my-service",
    "Version": "1.0.0",
    "Protocol": "HttpProtobuf",
    "Debug": false
  }
}
```

Required keys:

- `CollectorUri`
- `Service`

Notes:

- If `OTEL_EXPORTER_OTLP_ENDPOINT` is set, OpenTelemetry standard environment configuration is used.
- `Protocol` defaults to `Grpc` if omitted.
