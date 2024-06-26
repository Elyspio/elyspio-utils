using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;

public interface ITodoRepository : ICrudMongoRepository<TodoEntity, TodoBase>
{
	/// <summary>
	///     Toggle the checked state of a todo
	/// </summary>
	/// <param name="idTodo"></param>
	/// <returns></returns>
	Task Toggle(Guid idTodo);

	/// <summary>
	///     Get all todos of a user
	/// </summary>
	/// <param name="username"></param>
	/// <returns></returns>
	Task<List<TodoEntity>> GetByUsername(string username);
}