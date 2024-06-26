using System.Collections.Frozen;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;

namespace Elyspio.Utils.Telemetry.Technical.Options.Cache;

/// <summary>
///     Cache des options de capture pour la télémétrie
/// </summary>
public class TelemetryCaptureCache
{
	/// <inheritdoc cref="CaptureLevel" />
	public FrozenDictionary<CaptureLevel, bool> Levels { get; private set; } = new Dictionary<CaptureLevel, bool>
	{
		{ CaptureLevel.Enter, true },
		{ CaptureLevel.Exit, true },
		{ CaptureLevel.Warning, true },
		{ CaptureLevel.Information, true },
		{ CaptureLevel.Error, true },
		{ CaptureLevel.Debug, false }
	}.ToFrozenDictionary();

	/// <inheritdoc cref="CaptureHttp" />
	public FrozenDictionary<CaptureHttp, bool> Http { get; private set; } = new Dictionary<CaptureHttp, bool>
	{
		{ CaptureHttp.RequestBody, false },
		{ CaptureHttp.ResponseBody, false }
	}.ToFrozenDictionary();

	/// <inheritdoc cref="CaptureComponent" />
	public FrozenDictionary<CaptureComponent, bool> Components { get; private set; } = new Dictionary<CaptureComponent, bool>
	{
		{ CaptureComponent.Controller, true },
		{ CaptureComponent.Middleware, true },
		{ CaptureComponent.Consumer, true },
		{ CaptureComponent.Service, false },
		{ CaptureComponent.Adapter, false },
		{ CaptureComponent.Repository, false }
	}.ToFrozenDictionary();

    /// <summary>
    ///     Met à jour les options de capture
    /// </summary>
    /// <param name="components"></param>
    /// <param name="levels"></param>
    /// <param name="http"></param>
    public void Update(Dictionary<CaptureComponent, bool> components, Dictionary<CaptureLevel, bool> levels, Dictionary<CaptureHttp, bool> http)
	{
		Levels = levels.ToFrozenDictionary();
		Http = http.ToFrozenDictionary();
		Components = components.ToFrozenDictionary();
	}
}