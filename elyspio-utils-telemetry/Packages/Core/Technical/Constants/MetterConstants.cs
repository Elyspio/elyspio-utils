namespace Elyspio.Utils.Telemetry.Technical.Constants;

/// <summary>
///     Constants for metters
/// </summary>
public static class MetterConstants
{
	/// <summary>
	///     Default dotnet metters
	/// </summary>
	public static readonly string[] DefaultMetters =
	[
		"Microsoft.AspNetCore.Hosting",
		"Microsoft.AspNetCore.Http",
		"Microsoft.AspNetCore.Http.Connections",
		"Microsoft.AspNetCore.Server.Kestrel",
		"Microsoft.AspNetCore.RateLimiting",
		"Microsoft.AspNetCore.HeaderParsing",
		"Microsoft.AspNetCore.Routing",
		"Microsoft.AspNetCore.Diagnostics",
		"System.Net.NameResolution",
		"System.Net.Security",
		"System.Net.Sockets",
		"System.Net.Http"
	];
}