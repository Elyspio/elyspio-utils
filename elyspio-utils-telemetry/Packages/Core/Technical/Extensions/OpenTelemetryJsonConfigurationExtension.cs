using Elyspio.Utils.Telemetry.Technical.Configuration;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elyspio.Utils.Telemetry.Technical.Extensions;

/// <summary>Extensions for dynamically refreshing telemetry capture settings from JSON.</summary>
public static class OpenTelemetryJsonConfigurationExtension
{
	/// <summary>Registers the default <c>OpenTelemetry:Capture</c> configuration section.</summary>
	public static IServiceCollection AddOpenTelemetryJsonConfiguration(this IServiceCollection services, IConfiguration configuration, string section = "OpenTelemetry:Capture")
	{
		services.Configure<OpenTelemetryConfiguration>(configuration.GetSection(section));
		return services;
	}

	/// <summary>Applies the current configuration and keeps the telemetry cache synchronized on reload.</summary>
	public static WebApplication UseOpenTelemetryJsonConfiguration(this WebApplication app)
	{
		var options = app.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryConfiguration>>();
		void Apply(OpenTelemetryConfiguration value) => TracingContext.OpenTelemetryOptions.Cache.Update(value.Activated, value.Components.ToDictionary(), value.Levels.ToDictionary());
		options.OnChange(Apply);
		Apply(options.CurrentValue);
		return app;
	}
}
