using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Common.Extensions;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;
using Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Mongo.Base;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Mongo;

internal class TodoRepository(IConfiguration configuration, ILogger<BaseRepository<TodoEntity>> logger)
	: CrudMongoRepository<TodoEntity, TodoBase>(configuration, logger), ITodoRepository
{
	/// <inheritdoc />
	public async Task Toggle(Guid idTodo)
	{
		using var _ = LogRepository($"{Log.F(idTodo)}");

		var todo = await GetById(idTodo.AsObjectId());

		if (todo is null) throw new Exception($"Todo {idTodo} not found");

		todo.Checked = !todo.Checked;

		await EntityCollection.ReplaceOneAsync(t => t.Id == idTodo.AsObjectId(), todo);
	}

	public async Task<List<TodoEntity>> GetByUsername(string username)
	{
		using var _ = LogRepository($"{Log.F(username)}");
		return await EntityCollection.AsQueryable().Where(t => t.User == username).ToListAsync();
	}
}