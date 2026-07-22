using OpenTelemetry.Trace;
using Serilog;

namespace Elyspio.Utils.Telemetry.Tracing.Samplers;

/// <summary>Samples activities according to a caller-supplied predicate.</summary>
public sealed class DropByPropertiesSampler(Func<SamplingParameters, bool> filter) : Sampler
{
	/// <summary>Determines whether an activity should be sampled.</summary>
	/// <param name="samplingParameters">The activity sampling parameters.</param>
	/// <returns>A sampling decision based on the configured filter.</returns>
	public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
	{
		try { return new SamplingResult(filter(samplingParameters) ? SamplingDecision.RecordAndSample : SamplingDecision.Drop); }
		catch (Exception exception) { Log.Warning(exception, "Telemetry sampler failed; keeping activity"); return new SamplingResult(SamplingDecision.RecordAndSample); }
	}
}
