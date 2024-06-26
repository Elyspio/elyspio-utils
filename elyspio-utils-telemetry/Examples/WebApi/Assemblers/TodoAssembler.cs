using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Common.Extensions;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Common.Assemblers;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Assemblers;

public class TodoAssembler : BaseAssembler<Todo, TodoEntity>
{
	public override Todo Convert(TodoEntity obj)
	{
		return new Todo
		{
			Checked = obj.Checked,
			Id = obj.Id.AsGuid(),
			Label = obj.Label,
			User = obj.User
		};
	}

	public override TodoEntity Convert(Todo obj)
	{
		return new TodoEntity
		{
			Checked = obj.Checked,
			Id = obj.Id.AsObjectId(),
			Label = obj.Label,
			User = obj.User
		};
	}
}