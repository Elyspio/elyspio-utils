using Elyspio.Utils.Telemetry.MassTransit.Business.Enums;
using Elyspio.Utils.Telemetry.MassTransit.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using MassTransit;

namespace Elyspio.Utils.Telemetry.MassTransit.Tracing;

/// <summary>
///     Add tracing context to MassTransit consumer with <see cref="TracingController" />
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public abstract class TracingConsumer<TMessage> : IConsumer<TMessage> where TMessage : class
{
    /// <summary>
    ///     Consume the message with open telemetry context
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<TMessage> context)
	{
		MassTransitActivityHelper.SetActivityName<TMessage>(MassTransitOperation.Process);

		return ConsumeAsync(context);
	}


	/// <inheritdoc cref="Consume" />
	protected abstract Task ConsumeAsync(ConsumeContext<TMessage> context);
}