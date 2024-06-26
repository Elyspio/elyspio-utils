# Elyspio.Utils.Telemetry

Ce package permet d'ajouter des informations de télémétrie dans une application dotnet 7+.

## Installation


```bash
dotnet add package Elyspio.Utils.Telemetry
dotnet add package Elyspio.Utils.Telemetry.MongoDB
dotnet add package Elyspio.Utils.Telemetry.Sql
dotnet add package Elyspio.Utils.Telemetry.Redis
dotnet add package Elyspio.Utils.Telemetry.MassTransit
```

## Utilisation

Depuis la version 0.11.0, ce package inclut `Sisra.Socle.Logs.dotNetCore`, afin de récupérer l'idFlux depuis le context serilog

### Activation dans l'application

Dans le builder de l'application, il faut ajouter les services de télémétrie

```csharp   

if (builder.Configuration.IsTelemetryEnabled(out var telemetryOptions))
{
	var telemetryBuilder = new AppOpenTelemetryBuilder<Program>(telemetryOptions!)
	{
		Tracing = tracing => tracing
			.AddAppMongoInstrumentation()                         // Elyspio.Utils.MongoDB
			.AddAppSqlClientInstrumentation()                     // Elyspio.Utils.Sql
			.AddAppRedisInstrumentation()                         // Elyspio.Utils.Redis
			.AddAppMassTransitInstrumentation(),                  // Elyspio.Utils.MassTransit
		Meter = meter => meter.AddAppMassTransitInstrumentation() // Elyspio.Utils.MassTransit
	};
	telemetryBuilder.Build(builder.Services);
}
```

Une fois l'application builder créée, il faut activer les middlewares de Sisra.Socle.Logs.dotNetCore

```csharp
app.UseSisraSocleLogs();
```

### Gestion des traces de MongoDB

Pour avoir accès aux requêtes jouées dans Mongo il faut ajouter ce code lors de la création du client en plus d'utiliser l'extension `AddAppMongoInstrumentation`

```csharp
var mongoUrl = new MongoUrl(connectionString);
var clientSettings = MongoClientSettings.FromUrl(mongoUrl);

clientSettings.ClusterConfigurator = cb => { cb.Subscribe(new MongoDbActivityEventSubscriber()); };

var client = new MongoClient(clientSettings);
```

### Gestion des traces des consumers MassTransit

Pour avoir accès aux messages consommés par MassTransit il faut faire hériter les consumers de la
classe `TracingConsumer` et implementer la méthode `ConsumeAsync` (renommage de la méthode `Consume` de MassTransit)

Exemple :

```csharp
using Elyspio.Utils.Telemetry.MassTransit.Tracing;
using MassTransit;

public class ToggleTodoConsumer(ITodoRepository todoRepository) : TracingConsumer<ToggleTodoMessage> // remplace  IConsumer<ToggleTodoMessage>
{
	// Remplace public async Task Consume(ConsumeContext<ToggleTodoMessage> context)
	protected override async Task ConsumeAsync(ConsumeContext<ToggleTodoMessage> context)
	{
		// Do something
	}
}

```

### Configuration

Dans le fichier appsettings.json, ajouter la section suivante :

```json5
{
	"OpenTelemetry": {
		// Obligatoire
		"CollectorUri": "http://localhost:4318",
		// Obligatoire
		"Service": "aura-local-telemetry-webapi",
		// Optionnel
		"Version": "1.0.0",
		// Optionnel par défaut Grpc
		"Protocol": "HttpProtobuf",
		// Optionnel
		"Authentication": {
			"CertificatePemPath": "./client.pem", // Certificat pour faire du mTLS
			"CertificateKeyPath": "./client.key", // key du certificat pour faire du mTLS
		}
	}
}
```
