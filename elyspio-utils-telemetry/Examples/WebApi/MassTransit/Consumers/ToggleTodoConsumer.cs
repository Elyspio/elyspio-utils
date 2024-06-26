using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Examples.WebApi.MassTransit.Messages;
using Elyspio.Utils.Telemetry.MassTransit.Tracing;
using MassTransit;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.MassTransit.Consumers;

public class ToggleTodoConsumer(ITodoRepository todoRepository) : TracingConsumer<ToggleTodoMessage>
{
	protected override async Task ConsumeAsync(ConsumeContext<ToggleTodoMessage> context)
	{
		await todoRepository.Toggle(context.Message.IdTodo);
	}
}