using System.Collections.Frozen;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;

namespace Elyspio.Utils.Telemetry.Technical.Configuration;

/// <summary>Component capture settings read from configuration.</summary>
public sealed class CaptureComponentsConfiguration
{
	/// <summary>Gets or sets whether controller telemetry is captured.</summary>
	public bool Controllers { get; set; } = true;
	/// <summary>Gets or sets whether service telemetry is captured.</summary>
	public bool Services { get; set; } = true;
	/// <summary>Gets or sets whether attribute telemetry is captured.</summary>
	public bool Attributes { get; set; } = true;
	/// <summary>Gets or sets whether adapter telemetry is captured.</summary>
	public bool Adapters { get; set; } = true;
	/// <summary>Gets or sets whether repository telemetry is captured.</summary>
	public bool Repositories { get; set; } = true;
	/// <summary>Gets or sets whether consumer telemetry is captured.</summary>
	public bool Consumers { get; set; } = true;
	/// <summary>Gets or sets whether producer telemetry is captured.</summary>
	public bool Producers { get; set; } = true;
	/// <summary>Gets or sets whether middleware telemetry is captured.</summary>
	public bool Middleware { get; set; } = true;

	/// <summary>Converts the configured values into a lookup by capture component.</summary>
	/// <returns>The configured component capture values.</returns>
	public FrozenDictionary<CaptureComponent, bool> ToDictionary() => new Dictionary<CaptureComponent, bool>
	{
		[CaptureComponent.Controller] = Controllers, [CaptureComponent.Service] = Services,
		[CaptureComponent.Attribute] = Attributes, [CaptureComponent.Adapter] = Adapters,
		[CaptureComponent.Repository] = Repositories, [CaptureComponent.Consumer] = Consumers,
		[CaptureComponent.Producer] = Producers, [CaptureComponent.Middleware] = Middleware
	}.ToFrozenDictionary();
}
