using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Models;
using Elyspio.Utils.Telemetry.Examples.WebApi.Models.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Models.Entities;

public class TodoEntity : TodoBase, IEntity
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public ObjectId Id { get; set; }
}