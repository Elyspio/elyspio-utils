namespace Elyspio.Utils.Telemetry.Technical.Options.Capture;

/// <summary>
///     Determine si une activité doit être conservée par
/// </summary>
public class TelemetryCapture
{
	/// <summary>
	///     Fonction pour déterminer si les composants doivent être capturés
	/// </summary>
	public Func<CaptureComponent, IServiceProvider, bool> Components { get; set; } = DefaultComponents;

	/// <summary>
	///     Fonction pour déterminer si les niveaux de log doivent être capturés
	/// </summary>
	public Func<CaptureLevel, IServiceProvider, bool> Levels { get; set; } = DefaultLevels;


	/// <summary>
	///     Fonction pour déterminer si le contenu des requêtes / réponses doivent être capturés
	/// </summary>
	public Func<CaptureHttp, IServiceProvider, bool> Http { get; set; } = DefaultHttp;


	private static Func<CaptureComponent, IServiceProvider, bool> DefaultComponents => (component, _) => component switch
	{
		_ => true
	};

	private static Func<CaptureLevel, IServiceProvider, bool> DefaultLevels => (level, _) => level switch
	{
		_ => true
	};

	private static Func<CaptureHttp, IServiceProvider, bool> DefaultHttp => (level, _) => level switch
	{
		_ => false
	};
}