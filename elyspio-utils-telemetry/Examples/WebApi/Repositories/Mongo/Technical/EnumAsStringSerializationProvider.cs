using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Mongo.Technical;

/// <inheritdoc />
public sealed class EnumAsStringSerializationProvider : BsonSerializationProviderBase
{
	/// <inheritdoc />
	public override IBsonSerializer GetSerializer(Type type, IBsonSerializerRegistry serializerRegistry)
	{
		if (!type.IsEnum) return null!;

		var enumSerializerType = typeof(EnumSerializer<>).MakeGenericType(type);
		var enumSerializerConstructor = enumSerializerType.GetConstructor([
			typeof(BsonType)
		]);
		var enumSerializer = (IBsonSerializer?)enumSerializerConstructor?.Invoke([
			BsonType.String
		]);

		return enumSerializer!;
	}
}