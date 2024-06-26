using Microsoft.Extensions.Caching.Distributed;

namespace Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;

public interface IRedisCacheService
{
    /// <summary>
    ///     Get a value from the cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<T?> Get<T>(string key);

    /// <summary>
    ///     Set a value in the cache with a expiration time
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="ttl"></param>
    /// <returns></returns>
    public Task Set<T>(string key, T value, TimeSpan ttl);

    /// <summary>
    ///     Set a value in the cache with a complex expiration time
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="option"></param>
    /// <returns></returns>
    public Task Set<T>(string key, T value, DistributedCacheEntryOptions option);
}