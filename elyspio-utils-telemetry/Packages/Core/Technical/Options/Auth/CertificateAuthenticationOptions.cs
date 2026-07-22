namespace Elyspio.Utils.Telemetry.Technical.Options.Auth;

/// <summary>Certificate paths used to authenticate an OTLP HTTP exporter with mTLS.</summary>
public sealed class CertificateAuthenticationOptions
{
	/// <summary>Gets the path to the client certificate PEM file.</summary>
	public required string CertificatePemPath { get; init; }
	/// <summary>Gets the path to the client certificate private-key PEM file.</summary>
	public required string CertificateKeyPath { get; init; }
	/// <summary>Gets the path to the certificate-authority PEM file.</summary>
	public required string CaPemPath { get; init; }
}
