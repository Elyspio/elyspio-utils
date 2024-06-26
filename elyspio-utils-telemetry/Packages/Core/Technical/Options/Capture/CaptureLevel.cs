namespace Elyspio.Utils.Telemetry.Technical.Options.Capture;

/// <summary>
///     Determine comment les logs doivent être capturés
/// </summary>
[Flags]
public enum CaptureLevel
{
	/// <summary>
	///     Si les arguments fourni dans le log.enter sont capturés (attention, peut être sensible)
	/// </summary>
	Enter = 1,

	/// <summary>
	///     Si les arguments fourni dans le log.exit sont capturés (attention, peut être sensible)
	/// </summary>
	Exit = 2,

	/// <summary>
	///     Si les warnings sont capturés (attention, peut être sensible)
	/// </summary>
	Warning = 4,

	/// <summary>
	///     Si les logs d'information sont capturés (attention, peut être sensible)
	/// </summary>
	Information = 8,

	/// <summary>
	///     Si les logs de debug sont capturés (attention, peut être sensible)
	/// </summary>
	Debug = 16,

	/// <summary>
	///     Si les logs d'erreur sont capturés (attention, peut être sensible)
	/// </summary>
	Error = 32
}