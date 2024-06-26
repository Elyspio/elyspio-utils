using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;

public interface IUserRepository : ICrudSqlRepository<UserEntity, UserBase>
{
}