using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;

public interface ITodoService
{
	Task<Todo> Add(Guid idUser, string label);
	Task<List<Todo>> GetAll(Guid idUser);
	Task Delete(Guid idUser, Guid idTodo);
	Task Toggle(Guid idUser, Guid idTodo);
}