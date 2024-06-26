namespace Elyspio.Utils.Telemetry.Technical.Options.Auth;

/// <summary>
/// Configuration de l'authentification par certificat pour faire du mTLS
/// </summary>
/// <param name="CertificatePemPath">Chemin vers le certificat .pem</param>
/// <param name="CertificateKeyPath">Chemin vers la clé du certificat</param>
/// <param name="CaPemPath">Chemin vers la clé du certificat</param>
public sealed record CertificateAuthenticationOptions(string CertificatePemPath, string CertificateKeyPath, string CaPemPath);