using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.AspNetCore.Mvc;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Controllers;

[Route("api/todo")]
[ApiController]
public class TodoController(ITodoService todoService, ILogger<TodoController> logger) : TracingController(logger)
{
	[HttpGet]
	[ProducesResponseType(typeof(List<Todo>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(Guid idUser)
	{
		using var _ = LogController();
		return Ok(await todoService.GetAll(idUser));
	}


	[HttpPost]
	[ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
	public async Task<IActionResult> Add(Guid idUser, [FromBody] string label)
	{
		using var _ = LogController($"{Log.F(label)}");
		var todo = await todoService.Add(idUser, label);
		return Created($"api/todo/{todo.Id}", todo);
	}

    /// <summary>
    /// </summary>
    /// <param name="idUser">Id user</param>
    /// <param name="id">Id of the todo</param>
    /// <returns></returns>
    [HttpPut("{id:guid}/toggle")]
	[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Toggle(Guid idUser, Guid id)
	{
		using var _ = LogController($"{Log.F(idUser)} {Log.F(id)}");
		await todoService.Toggle(idUser, id);
		return NoContent();
	}

	[HttpDelete("{id:guid}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(Guid idUser, Guid id)
	{
		using var _ = LogController($"{Log.F(id)}");
		await todoService.Delete(idUser, id);
		return NoContent();
	}
}