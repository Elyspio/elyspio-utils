﻿using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.ApiSante.Rest;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.AspNetCore.Mvc;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Controllers;

[Route("api/users")]
[ApiController]
public class UserController(IUserService userService, ILogger<UserController> logger) : TracingController(logger)
{
	[HttpGet("search")]
	[ProducesResponseType(typeof(List<UtilisateurActeurLight>), StatusCodes.Status200OK)]
	public async Task<IActionResult> Search(string prenomNom)
	{
		using var _ = LogController($"{Log.F(prenomNom)}");
		return Ok(await userService.SearchPs(prenomNom));
	}

	[HttpGet]
	[ProducesResponseType(typeof(List<User>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll()
	{
		using var _ = LogController();
		return Ok(await userService.GetAll());
	}

	[HttpGet("{idUser:guid}")]
	[ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(Guid idUser)
	{
		using var _ = LogController($"{Log.F(idUser)}");
		return Ok(await userService.GetById(idUser));
	}


	[HttpPost]
	[ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
	public async Task<IActionResult> Add([FromBody] string username)
	{
		using var _ = LogController($"{Log.F(username)}");
		var user = await userService.Add(username);
		return Created($"/api/users/{user.Id}", user);
	}

	[HttpDelete("{idUser:guid}")]
	[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(Guid idUser)
	{
		using var _ = LogController($"{Log.F(idUser)}");
		await userService.Delete(idUser);
		return NoContent();
	}
}