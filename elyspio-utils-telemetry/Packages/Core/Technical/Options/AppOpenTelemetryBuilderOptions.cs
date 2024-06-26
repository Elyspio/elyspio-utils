using Elyspio.Utils.Telemetry.Technical.Options.Auth;
using Elyspio.Utils.Telemetry.Technical.Options.Cache;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using OpenTelemetry.Exporter;

namespace Elyspio.Utils.Telemetry.Technical.Options;

/// <summary>
///     Options for OpenTelemetryBuilder
/// </summary>
public sealed class AppOpenTelemetryBuilderOptions
{
    /// <summary>
    ///     Address of the OpenTelemetry collector
    /// </summary>
    public required Uri CollectorUri { get; init; }

    /// <summary>
    ///     Service name
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    ///     Service version
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    ///     Log telemetry internal errors to console
    /// </summary>
    public bool? Debug { get; init; } = false;

    /// <summary>
    ///     Protocol used to communicate with the collector
    /// </summary>
    public OtlpExportProtocol Protocol { get; init; } = OtlpExportProtocol.Grpc;

    /// <summary>
    ///     Determine if a component sould be traced
    /// </summary>
    public TelemetryCapture ShouldCapture { get; init; } = new();

    /// <summary>
    ///     Cache de l'activation des logs levels et des logs des http body (hors DI)
    /// </summary>
    public TelemetryCaptureCache CaptureCache { get; } = new();

    /// <summary>
    ///     Authentication options
    /// </summary>
    public CertificateAuthenticationOptions? Authentication { get; init; }
}