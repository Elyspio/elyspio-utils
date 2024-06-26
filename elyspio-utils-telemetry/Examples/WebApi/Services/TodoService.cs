using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Common.Extensions;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.Assemblers;
using Elyspio.Utils.Telemetry.Examples.WebApi.MassTransit.Messages;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using MassTransit;

namespace Coexya.Utils.Telemetry.Examples.WebApi.Services;

public class TodoService(ITodoRepository todoRepository, IUserService userService, ILogger<TodoService> logger, IBusControl bus) : TracingService(logger), ITodoService
{
	private readonly TodoAssembler _todoAssembler = new();

	public async Task<Todo> Add(Guid idUser, string label)
	{
		using var _ = LogService($"{Log.F(idUser)} {Log.F(label)}");

		var entity = await todoRepository.Add(new TodoBase
		{
			Checked = false,
			Label = label,
			User = await userService.GetUsername(idUser)
		});

		return _todoAssembler.Convert(entity);
	}

	public async Task<List<Todo>> GetAll(Guid idUser)
	{
		using var _ = LogService($"{Log.F(idUser)}");


		var entities = await todoRepository.GetByUsername(await userService.GetUsername(idUser));

		return _todoAssembler.Convert(entities);
	}


	public async Task Delete(Guid idUser, Guid idTodo)
	{
		using var _ = LogService($"{Log.F(idUser)} {Log.F(idTodo)}");

		var todo = await todoRepository.GetById(idTodo.AsObjectId());

		var username = await userService.GetUsername(idUser);

		if (todo is null) throw new Exception("User not found");

		if (username != todo.User) throw new Exception("This is not your todo");

		await todoRepository.Delete(idTodo.AsObjectId());
	}

	public async Task Toggle(Guid idUser, Guid idTodo)
	{
		using var _ = LogService($"{Log.F(idUser)} {Log.F(idTodo)}");

		var todo = await todoRepository.GetById(idTodo.AsObjectId());

		var username = await userService.GetUsername(idUser);

		if (todo is null) throw new Exception("Todo not found");

		if (username != todo.User) throw new Exception("This is not your todo");

		await bus.Publish(new ToggleTodoMessage(idTodo));
	}
}