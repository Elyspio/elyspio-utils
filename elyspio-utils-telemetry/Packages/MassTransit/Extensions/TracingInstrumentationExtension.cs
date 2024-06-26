using Elyspio.Utils.Telemetry.MassTransit.Processors;
using Elyspio.Utils.Telemetry.MassTransit.Samplers;
using MassTransit.Monitoring;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Elyspio.Utils.Telemetry.MassTransit.Extensions;

/// <summary>
///     Extensions for sql client instrumentation
/// </summary>
public static class TracingInstrumentationExtension
{
	/// <summary>
	///     Add sql client instrumentation
	/// </summary>
	/// <param name="builder"></param>
	/// <returns></returns>
	public static TracerProviderBuilder AddAppMassTransitInstrumentation(this TracerProviderBuilder builder)
	{
		builder.AddSource(InstrumentationOptions.MeterName); // MassTransit ActivitySource
		builder.AddProcessor<MassTransitProcessor>();
		builder.SetSampler<MassTransitSampler>();

		return builder;
	}


	/// <summary>
	///     Add sql client instrumentation
	/// </summary>
	/// <param name="builder"></param>
	/// <returns></returns>
	public static MeterProviderBuilder AddAppMassTransitInstrumentation(this MeterProviderBuilder builder)
	{
		builder.AddMeter("MassTransit");

		return builder;
	}
}