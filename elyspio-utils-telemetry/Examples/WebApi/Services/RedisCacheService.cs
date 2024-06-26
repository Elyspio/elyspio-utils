using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Tracing.Elements;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Services;

/// <inheritdoc cref="IRedisCacheService" />
public class RedisCacheService : TracingService, IRedisCacheService
{
	private readonly IDistributedCache cache;

	public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : base(logger)
	{
		this.cache = cache;
	}


	/// <inheritdoc />
	public async Task<T?> Get<T>(string key)
	{
		try
		{
			using var _ = LogService(Log.F(key));

			var valueType = typeof(T);

			var value = await cache.GetStringAsync(key);

			if (value == null) return (T)Convert.ChangeType(null, valueType)!;

			T data;
			if (valueType.IsEnum) data = (T)Enum.Parse(valueType, value);
			else if (valueType.IsPrimitive) data = (T)Convert.ChangeType(value, valueType);
			else data = JsonConvert.DeserializeObject<T>(value)!;

			return data;
		}
		catch (Exception e)
		{
			_logger.LogWarning(e, "Erreur d'accès au cache");
			return default;
		}
	}

	/// <inheritdoc />
	public async Task Set<T>(string key, T value, TimeSpan ttl)
	{
		using var _ = LogService($"{Log.F(key)} {Log.F(ttl.TotalSeconds)}");

		await Set(key, value, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = ttl
		});
	}

	/// <inheritdoc />
	public async Task Set<T>(string key, T value, DistributedCacheEntryOptions option)
	{
		try
		{
			using var _ = LogService($"{Log.F(key)} {Log.F(option)}");

			await cache.SetStringAsync(key, JsonConvert.SerializeObject(value, new StringEnumConverter()), option);
		}
		catch (Exception e)
		{
			_logger.LogWarning(e, "Erreur d'enregistrement de la donnée dans le cache");
		}
	}
}