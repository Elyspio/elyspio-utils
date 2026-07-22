namespace Elyspio.Utils.Telemetry.Technical.Configuration;

/// <summary>Dynamic capture configuration under <c>OpenTelemetry:Capture</c>.</summary>
public sealed class OpenTelemetryConfiguration
{
	/// <summary>Gets or sets the component capture settings.</summary>
	public CaptureComponentsConfiguration Components { get; set; } = new();
	/// <summary>Gets or sets the capture-level settings.</summary>
	public CaptureLevelsConfiguration Levels { get; set; } = new();
	/// <summary>Gets or sets whether telemetry capture is active.</summary>
	public bool Activated { get; set; }
}
