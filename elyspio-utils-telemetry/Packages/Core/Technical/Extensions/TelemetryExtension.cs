using Elyspio.Utils.Telemetry.Technical.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

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
	[PublicAPI]
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


	/// <summary>
	///    Active Serilog avec la gestion des traces OpenTelemetry ssi <see cref="UseStandardConfiguration"/> est vrai
	/// </summary>
	/// <param name="host"></param>
	/// <param name="configureLogger"></param>
	/// <returns></returns>
	[PublicAPI]
	public static IHostBuilder UseSerilogWithTelemetry(this ConfigureHostBuilder host, Action<HostBuilderContext, LoggerConfiguration>? configureLogger = null)
	{
		host.UseSerilog((context, configuration) =>
		{
			var conf = configuration.ReadFrom.Configuration(context.Configuration)
				.Enrich.FromLogContext();

			if (UseStandardConfiguration)
			{
				conf.WriteTo.OpenTelemetry();
			}

			configureLogger?.Invoke(context, configuration);
		});

		return host;
	}

	/// <summary>
	///    Indique si la config doit provient en priorité des variables d'env de la norme OpenTelemetry
	/// </summary>
	[PublicAPI]
	public static bool UseStandardConfiguration => Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") is not null;
}