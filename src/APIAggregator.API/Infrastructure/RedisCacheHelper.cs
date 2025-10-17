using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class RedisCacheHelper
{
	private readonly IDistributedCache _cache;

	public RedisCacheHelper(IDistributedCache cache)
	{
		_cache = cache;
	}

	public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
	{
		var options = new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(10)
		};

		var json = JsonSerializer.Serialize(value);
		await _cache.SetStringAsync(key, json, options);
	}

	public async Task<T?> GetAsync<T>(string key)
	{
		var json = await _cache.GetStringAsync(key);
		if (string.IsNullOrEmpty(json)) return default;
		return JsonSerializer.Deserialize<T>(json);
	}

	public async Task RemoveAsync(string key)
	{
		await _cache.RemoveAsync(key);
	}
}
