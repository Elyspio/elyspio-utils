using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Models;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;

public class UserEntity : UserBase, ISqlEntity
{
	public Guid Id { get; set; }
}