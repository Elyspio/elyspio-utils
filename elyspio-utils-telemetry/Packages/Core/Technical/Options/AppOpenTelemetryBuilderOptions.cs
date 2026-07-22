using Elyspio.Utils.Telemetry.Technical.Options.Auth;
using Elyspio.Utils.Telemetry.Technical.Options.Cache;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using OpenTelemetry.Exporter;

namespace Elyspio.Utils.Telemetry.Technical.Options;

/// <summary>Options used to configure OpenTelemetry.</summary>
public sealed class AppOpenTelemetryBuilderOptions
{
	/// <summary>Gets the OTLP collector endpoint.</summary>
	public required Uri CollectorUri { get; init; }
	/// <summary>Gets the service name reported by telemetry.</summary>
	public required string Service { get; init; }
	/// <summary>Gets the optional service version reported by telemetry.</summary>
	public string? Version { get; init; }
	/// <summary>Gets whether debug telemetry is enabled.</summary>
	public bool? Debug { get; init; } = false;
	/// <summary>Gets whether the application is running in a test environment.</summary>
	public bool? RunningInTestEnvironment { get; init; } = false;
	/// <summary>Gets the OTLP transport protocol.</summary>
	public OtlpExportProtocol Protocol { get; init; } = OtlpExportProtocol.Grpc;
	/// <summary>Gets the configured telemetry capture settings.</summary>
	public TelemetryCapture ShouldCapture { get; init; } = new();
	/// <summary>Gets the dynamically refreshable telemetry capture settings.</summary>
	public TelemetryCaptureCache Cache { get; } = new();
	/// <summary>Backward-compatible alias for <see cref="Cache"/>.</summary>
	public TelemetryCaptureCache CaptureCache => Cache;
	/// <summary>Gets the optional certificate authentication settings for the exporter.</summary>
	public CertificateAuthenticationOptions? Authentication { get; init; }
}
