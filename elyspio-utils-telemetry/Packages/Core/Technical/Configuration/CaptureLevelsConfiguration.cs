using System.Collections.Frozen;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;

namespace Elyspio.Utils.Telemetry.Technical.Configuration;

/// <summary>Log and detailed-instrumentation capture settings read from configuration.</summary>
public sealed class CaptureLevelsConfiguration
{
	/// <summary>Gets or sets whether entry events are captured.</summary>
	public bool Enter { get; set; } = true;
	/// <summary>Gets or sets whether exit events are captured.</summary>
	public bool Exit { get; set; } = true;
	/// <summary>Gets or sets whether warning events are captured.</summary>
	public bool Warning { get; set; } = true;
	/// <summary>Gets or sets whether informational events are captured.</summary>
	public bool Information { get; set; } = true;
	/// <summary>Gets or sets whether debug events are captured.</summary>
	public bool Debug { get; set; } = true;
	/// <summary>Gets or sets whether error events are captured.</summary>
	public bool Error { get; set; } = true;
	/// <summary>Gets or sets whether trace events are captured.</summary>
	public bool Trace { get; set; }

	/// <summary>Converts the configured values into a lookup by capture level.</summary>
	/// <returns>The configured capture-level values.</returns>
	public FrozenDictionary<CaptureLevel, bool> ToDictionary() => new Dictionary<CaptureLevel, bool>
	{
		[CaptureLevel.Enter] = Enter, [CaptureLevel.Exit] = Exit, [CaptureLevel.Warning] = Warning,
		[CaptureLevel.Information] = Information, [CaptureLevel.Debug] = Debug, [CaptureLevel.Error] = Error,
		[CaptureLevel.Trace] = Trace
	}.ToFrozenDictionary();
}
