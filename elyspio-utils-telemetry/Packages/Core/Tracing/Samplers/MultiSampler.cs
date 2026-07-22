using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using OpenTelemetry.Trace;

namespace Elyspio.Utils.Telemetry.Tracing.Samplers;

/// <summary>Combines custom samplers and honours dynamic capture activation.</summary>
public sealed class MultiSampler(IEnumerable<Sampler> samplers) : Sampler
{
	private readonly Sampler[] _samplers = samplers.ToArray();

	/// <summary>Determines whether an activity should be sampled by every configured sampler.</summary>
	/// <param name="samplingParameters">The activity sampling parameters.</param>
	/// <returns>A sampling decision that honours dynamic capture activation.</returns>
	public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
	{
		if (!TracingContext.OpenTelemetryOptions.Cache.Activated) return new SamplingResult(SamplingDecision.Drop);
		foreach (var sampler in _samplers)
		{
			if (sampler.ShouldSample(samplingParameters).Decision == SamplingDecision.Drop) return new SamplingResult(SamplingDecision.Drop);
		}
		return new SamplingResult(SamplingDecision.RecordAndSample);
	}
}
