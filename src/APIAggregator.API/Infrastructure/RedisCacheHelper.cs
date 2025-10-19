using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

/// <summary>
/// Provides helper methods for interacting with a Redis-based distributed cache.
/// </summary>
/// <remarks>This class simplifies common caching operations such as adding, retrieving, and removing items from a
/// Redis cache. It uses <see cref="IDistributedCache"/> for cache operations and logs warnings for any failures. <para>
/// By default, cached items are stored with a time-to-live (TTL) of 10 minutes unless a custom TTL is specified.
/// </para></remarks>
public class RedisCacheHelper
{
	private readonly IDistributedCache _cache;
	private readonly ILogger<RedisCacheHelper> _logger;

	public RedisCacheHelper(IDistributedCache cache, ILogger<RedisCacheHelper> logger)
	{
		_cache = cache;
		_logger = logger;
	}

	/// <summary>
	/// Asynchronously sets a value in the distributed cache with the specified key and optional time-to-live (TTL).
	/// </summary>
	/// <remarks>The value is serialized to JSON before being stored in the cache. If an error occurs during the
	/// operation, it is logged, and the method returns <see langword="false"/>.</remarks>
	/// <typeparam name="T">The type of the value to cache. The value will be serialized to JSON before being stored.</typeparam>
	/// <param name="key">The unique key used to identify the cached value. Cannot be <see langword="null"/> or empty.</param>
	/// <param name="value">The value to cache. Cannot be <see langword="null"/>.</param>
	/// <param name="ttl">An optional time-to-live (TTL) for the cached value. If not specified, a default TTL of 10 minutes is used.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the value was
	/// successfully cached; otherwise, <see langword="false"/> if an error occurred.</returns>
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

	/// <summary>
	/// Retrieves a cached value associated with the specified key, deserializing it to the specified type.
	/// </summary>
	/// <remarks>If the key does not exist in the cache or the cached value is empty, the method returns the default
	/// value  for the specified type. If deserialization fails, the method logs a warning and also returns the default
	/// value.</remarks>
	/// <typeparam name="T">The type to which the cached value should be deserialized.</typeparam>
	/// <param name="key">The key identifying the cached value. Cannot be <see langword="null"/> or empty.</param>
	/// <returns>The deserialized value of type <typeparamref name="T"/> if the key exists and the value is not empty;  otherwise,
	/// the default value for type <typeparamref name="T"/>.</returns>
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

	/// <summary>
	/// Removes the cache item associated with the specified key asynchronously.
	/// </summary>
	/// <remarks>If an error occurs during the removal process, the method logs a warning and returns <see
	/// langword="false"/>.</remarks>
	/// <param name="key">The key of the cache item to remove. Cannot be <see langword="null"/> or empty.</param>
	/// <returns><see langword="true"/> if the cache item was successfully removed; otherwise, <see langword="false"/>.</returns>
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
