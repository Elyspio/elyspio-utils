using Elyspio.Utils.Telemetry.Examples.WebApi.ApiSante.Rest;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Transports;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;

public interface IUserService
{
	Task Delete(Guid idUser);
	Task<User> Add(string username);
	Task<List<User>> GetAll();
	Task<User> GetById(Guid idUser);

	Task<string> GetUsername(Guid idUser);

	public Task<IReadOnlyCollection<UtilisateurActeurLight>> SearchPs(string prenomNom);
}