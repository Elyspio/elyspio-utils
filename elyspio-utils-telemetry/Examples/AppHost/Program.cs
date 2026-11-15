var builder = DistributedApplication.CreateBuilder(args);

// Aspire injects OTEL_EXPORTER_OTLP_ENDPOINT/PROTOCOL/HEADERS and OTEL_SERVICE_NAME: traces, metrics and logs land in the dashboard.
builder.AddProject<Projects.Elyspio_Utils_Telemetry_Examples_WebApi>("webapi")
	.WithHttpHealthCheck("/health");

builder.Build().Run();
