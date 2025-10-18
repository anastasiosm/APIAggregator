using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class RedisCacheHelper
{
	private readonly IDistributedCache _cache;
	private readonly ILogger<RedisCacheHelper> _logger;

	public RedisCacheHelper(IDistributedCache cache, ILogger<RedisCacheHelper> logger)
	{
		_cache = cache;
		_logger = logger;
	}

	public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? ttl = null)
	{
		try
		{
			var options = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(10)
			};

			var json = JsonSerializer.Serialize(value);
			await _cache.SetStringAsync(key, json, options);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to cache item with key: {Key}", key);
			return false;
		}
	}

	public async Task<T?> GetAsync<T>(string key)
	{
		try
		{
			var json = await _cache.GetStringAsync(key);
			return string.IsNullOrEmpty(json)
				? default
				: JsonSerializer.Deserialize<T>(json);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to read from cache for key: {Key}", key);
			return default;
		}
	}

	public async Task<bool> RemoveAsync(string key)
	{
		try
		{
			await _cache.RemoveAsync(key);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to remove cache item with key: {Key}", key);
			return false;
		}
	}
}
