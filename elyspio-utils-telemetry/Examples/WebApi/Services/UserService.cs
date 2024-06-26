using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.ApiSante.Rest;
using Elyspio.Utils.Telemetry.Examples.WebApi.Assemblers;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Services;

public class UserService : TracingService, IUserService
{
	private readonly ApiSanteRestClient _apiSanteRestClient;
	private readonly IRedisCacheService _cache;
	private readonly UserAssembler _userAssembler = new();
	private readonly IUserRepository _userRepository;

	public UserService(ILogger<UserService> logger, IUserRepository userRepository, IRedisCacheService cache,
		ApiSanteRestClient apiSanteRestClient) : base(logger)
	{
		_userRepository = userRepository;
		_cache = cache;
		_apiSanteRestClient = apiSanteRestClient;
	}

	public async Task Delete(Guid idUser)
	{
		using var _ = LogService($"{Log.F(idUser)}");

		await _userRepository.Delete(idUser);
	}

	public async Task<User> Add(string username)
	{
		using var _ = LogService($"{Log.F(username)}");

		var entity = await _userRepository.Add(new UserBase
		{
			Username = username
		});

		return _userAssembler.Convert(entity);
	}

	public async Task<List<User>> GetAll()
	{
		using var _ = LogService();

		var entities = await _userRepository.GetAll();

		return _userAssembler.Convert(entities);
	}

	public async Task<User> GetById(Guid idUser)
	{
		using var _ = LogService($"{Log.F(idUser)}");

		var userFromCache = await _cache.Get<User>(idUser.ToString());
		if (userFromCache is not null) return userFromCache;

		var entity = await _userRepository.GetById(idUser);

		if (entity is null) throw new Exception("User not found");

		var user = _userAssembler.Convert(entity);

		await _cache.Set(idUser.ToString(), user, TimeSpan.FromMinutes(1));

		return user;
	}

	public async Task<string> GetUsername(Guid idUser)
	{
		using var _ = LogService($"{Log.F(idUser)}");

		var user = await GetById(idUser);

		if (user is null) throw new Exception("User not found");

		return user.Username;
	}

	public async Task<IReadOnlyCollection<UtilisateurActeurLight>> SearchPs(string prenomNom)
	{
		using var _ = LogService($"{Log.F(prenomNom)}");

		var user = await _apiSanteRestClient.Professionnels_FindPsByCriteriaAsync("slavy", new SearchRequestModel
		{
			NomsPrenom = prenomNom,
			MaxResults = 10,
			IdsTechAexclure = ArraySegment<string>.Empty
		});

		return user.ToList();
	}
}