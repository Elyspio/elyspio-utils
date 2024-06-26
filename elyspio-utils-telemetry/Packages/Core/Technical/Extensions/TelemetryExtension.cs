using Elyspio.Utils.Telemetry.Technical.Options;
using Microsoft.Extensions.Configuration;

namespace Elyspio.Utils.Telemetry.Technical.Extensions;

/// <summary>
///     Telemetry extensions
/// </summary>
public static class TelemetryExtension
{
	/// <summary>
	///     Check if telemetry is enabled and return the options
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="options"></param>
	/// <param name="section"></param>
	/// <returns></returns>
	public static bool IsTelemetryEnabled(this IConfiguration configuration, out AppOpenTelemetryBuilderOptions? options, string section = "OpenTelemetry")
	{
		try
		{
			options = configuration.GetSection(section).Get<AppOpenTelemetryBuilderOptions>();

			return options?.CollectorUri is not null && !string.IsNullOrWhiteSpace(options.Service);
		}
		catch (Exception)
		{
			options = null;
			return false;
		}
	}
}