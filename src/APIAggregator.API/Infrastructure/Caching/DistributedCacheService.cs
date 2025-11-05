using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace APIAggregator.API.Infrastructure.Caching;

public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedJson = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedJson))
                return default;

            return JsonSerializer.Deserialize<T>(cachedJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _cache.SetStringAsync(key, serialized, options, cancellationToken);
            _logger.LogDebug("Successfully cached result for key {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache result for key {CacheKey}", key);
        }
    }
}