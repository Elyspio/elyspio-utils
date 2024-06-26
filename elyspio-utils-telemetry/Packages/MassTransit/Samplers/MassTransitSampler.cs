using System.Diagnostics;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using OpenTelemetry.Trace;

namespace Elyspio.Utils.Telemetry.MassTransit.Samplers;

/// <summary>
/// Drop les activités MassTransit si la capture des composants de type Consumer ou Producer est désactivée
/// </summary>
public class MassTransitSampler : Sampler
{
	/// <inheritdoc />
	public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
	{
		if (samplingParameters.Kind == ActivityKind.Consumer && !TracingContext.OpenTelemetryOptions.CaptureCache.Components[CaptureComponent.Consumer])
		{
			return new SamplingResult(SamplingDecision.Drop);
		}

		if (samplingParameters.Kind == ActivityKind.Producer && !TracingContext.OpenTelemetryOptions.CaptureCache.Components[CaptureComponent.Producer])
		{
			return new SamplingResult(SamplingDecision.Drop);
		}

		return new SamplingResult(SamplingDecision.RecordAndSample);
	}
}