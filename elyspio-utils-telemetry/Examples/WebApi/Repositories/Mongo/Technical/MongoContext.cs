using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Mongo.Technical;

/// <summary>
///     Manage app mongo connection
/// </summary>
public sealed class MongoContext
{
	static MongoContext()
	{
		var pack = new ConventionPack
		{
			new EnumRepresentationConvention(BsonType.String)
		};
		ConventionRegistry.Register("EnumStringConvention", pack, _ => true);
		BsonSerializer.RegisterSerializationProvider(new EnumAsStringSerializationProvider());
	}

    /// <summary>
    ///     Default constructor
    /// </summary>
    /// <param name="configuration"></param>
    public MongoContext(IConfiguration configuration)
	{
		var connectionString = configuration["Mongo"]!;

		if (!Databases.TryGetValue(connectionString, out var value))
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

			var (client, url) = MongoClientFactory.Create(connectionString);

			Console.WriteLine($"Connecting to Database '{url.DatabaseName}'");
			value = client.GetDatabase(url.DatabaseName);
			Databases[connectionString] = value;
		}

		MongoDatabase = value;
	}

	private static ConcurrentDictionary<string, IMongoDatabase> Databases { get; } = new();

    /// <summary>
    ///     Récupération de la IMongoDatabase
    /// </summary>
    /// <returns></returns>
    public IMongoDatabase MongoDatabase { get; }
}