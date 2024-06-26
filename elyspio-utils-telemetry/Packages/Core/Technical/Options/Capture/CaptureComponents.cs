namespace Elyspio.Utils.Telemetry.Technical.Options.Capture;

/// <summary>
///     Determine si un component doit être tracé
/// </summary>
[Flags]
public enum CaptureComponent
{
    /// <summary>
    ///     Si les activités des controllers doivent être tracées
    /// </summary>
    Controller = 1,

    /// <summary>
    ///     Si les activités des attributs doivent être tracées
    /// </summary>
    Attribute = 1 << 2,

    /// <summary>
    ///     Si les activités des middlewares doivent être tracées
    /// </summary>
    Middleware = 1 << 3,

    /// <summary>
    ///     Si les activités des consumers doivent être tracées
    /// </summary>
    Consumer = 1 << 4,

    /// <summary>
    ///     Si les activités des services doivent être tracées
    /// </summary>
    Service = 1 << 5,

    /// <summary>
    ///     Si les activités des adapters doivent être tracées
    /// </summary>
    Adapter = 1 << 6,

    /// <summary>
    ///     Si les activités des repositories doivent être tracées
    /// </summary>
    Repository = 1 << 7,

    /// <summary>
    ///     Si les activités des producers doivent être tracées
    /// </summary>
    Producer = 1 << 8
}