using APIAggregator.API.Extensions;
using APIAggregator.API.Features.ExternalAPIs;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace APIAggregator.API.Features.Aggregation
{
	public class AggregationService : IAggregationService
	{
		private readonly IIpGeolocationClient _geoClient;
		private readonly IEnumerable<ILocationDataProvider> _providers;
		private readonly IDistributedCache _cache;
		private readonly ILogger<AggregationService> _logger;

		public AggregationService(
			IIpGeolocationClient geoClient,
			IEnumerable<ILocationDataProvider> providers,
			IDistributedCache cache,
		 	ILogger<AggregationService> logger)
		{
			_geoClient = geoClient;
			_providers = providers;
			_cache = cache;
			_logger = logger;
		}

		public async Task<AggregatedItemDto> GetAggregatedData(
			string ip,
			string? category = null,
			string? sortBy = null,
			bool descending = false,
			CancellationToken cancellationToken = default)
		{
			var cacheKey = $"Aggregated:{ip}";

			// 1. Try to get from cache with error handling
			AggregatedItemDto? cachedResult = null;
			try
			{
				var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
				if (!string.IsNullOrEmpty(cachedJson))
				{
					cachedResult = JsonSerializer.Deserialize<AggregatedItemDto>(cachedJson);
					if (cachedResult != null)
					{
						_logger.LogInformation("Returning cached result for {Ip}", ip);
						return cachedResult;
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}. Continuing without cache.", cacheKey);
				// Continue without cache - don't throw
			}

			// 2. Get location from IP
			var location = await _geoClient.GetLocationByIpAsync(ip, cancellationToken);
			if (location == null)
				throw new InvalidOperationException("Could not determine location from IP.");

			// 3. Call other APIs in parallel
			var tasks = _providers.ToDictionary(
				p => p.Name,
				p => p.GetDataAsync(location.Latitude, location.Longitude, cancellationToken));

			await Task.WhenAll(tasks.Values);

			// 4. Aggregate results
			var results = tasks.ToDictionary(
				task => task.Key,
				task => (object?)task.Value.Result // Task completed. So, calling .Result is not blocking!
			);

			var aggregated = new AggregatedItemDto
			{
				City = location.City,
				Country = location.Country,
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				Data = results
			};

			// 5. Apply filtering & sorting for providers implementing IFilterable
			aggregated.ApplyFilteringAndSorting(category);

			// 6. Try to cache the result with error handling
			try
			{
				var serialized = JsonSerializer.Serialize(aggregated);
				var cacheOptions = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
				};
				await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
				_logger.LogInformation("Successfully cached result for {Ip}", ip);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to cache result for key {CacheKey}. Continuing without caching.", cacheKey);
				// Continue without caching - don't throw
			}

			return aggregated;
		}		
	}
}
