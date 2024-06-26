namespace Elyspio.Utils.Telemetry.Technical.Options.Capture;

/// <summary>
///     Determine si le body des requêtes et réponses HTTP doivent être capturés
/// </summary>
[Flags]
public enum CaptureHttp
{
    /// <summary>
    ///     Détermine si le body de la requête doit être ajouté à l'activité
    /// </summary>
    RequestBody = 1,

    /// <summary>
    ///     Détermine si le body de la réponse doit être ajouté à l'activité
    /// </summary>
    ResponseBody = 1 << 2
}