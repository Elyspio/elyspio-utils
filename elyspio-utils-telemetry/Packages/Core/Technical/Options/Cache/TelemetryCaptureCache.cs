using System.Collections.Frozen;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;

namespace Elyspio.Utils.Telemetry.Technical.Options.Cache;

/// <summary>Fast, dynamically refreshable capture settings.</summary>
public class TelemetryCaptureCache
{
	/// <summary>Gets the enabled state for each capture level.</summary>
	public FrozenDictionary<CaptureLevel, bool> Levels { get; private set; } = new Dictionary<CaptureLevel, bool>
	{
		{ CaptureLevel.Enter, true }, { CaptureLevel.Exit, true }, { CaptureLevel.Warning, true },
		{ CaptureLevel.Information, true }, { CaptureLevel.Error, true }, { CaptureLevel.Debug, false }, { CaptureLevel.Trace, false }
	}.ToFrozenDictionary();

	/// <summary>Gets the enabled state for each capture component.</summary>
	public FrozenDictionary<CaptureComponent, bool> Components { get; private set; } = new Dictionary<CaptureComponent, bool>
	{
		{ CaptureComponent.Controller, true }, { CaptureComponent.Middleware, true }, { CaptureComponent.Consumer, true },
		{ CaptureComponent.Service, false }, { CaptureComponent.Adapter, false }, { CaptureComponent.Repository, false }
	}.ToFrozenDictionary();

	/// <summary>Gets whether telemetry capture is active.</summary>
	public bool Activated { get; private set; }

	/// <summary>Replaces the cached capture settings.</summary>
	/// <param name="activated">Whether telemetry capture is active.</param>
	/// <param name="components">The component capture settings.</param>
	/// <param name="levels">The capture-level settings.</param>
	public void Update(bool activated, FrozenDictionary<CaptureComponent, bool> components, FrozenDictionary<CaptureLevel, bool> levels)
	{
		Activated = activated;
		Components = components;
		Levels = levels;
	}
}
