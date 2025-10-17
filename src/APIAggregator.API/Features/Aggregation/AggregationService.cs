using APIAggregator.API.Extensions;
using APIAggregator.API.Features.ExternalAPIs;
using APIAggregator.API.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace APIAggregator.API.Features.Aggregation
{
	public class AggregationService : IAggregationService
	{
		private readonly IIpGeolocationClient _geoClient;
		private readonly IEnumerable<ILocationDataProvider> _providers;
		private readonly IDistributedCache _cache;

		public AggregationService(
			IIpGeolocationClient geoClient,
			IEnumerable<ILocationDataProvider> providers,
			IDistributedCache cache)
		{
			_geoClient = geoClient;
			_providers = providers;
			_cache = cache;
		}

		public async Task<AggregatedItemDto> GetAggregatedData(
			string ip,
			string? category = null,
			string? sortBy = null,
			bool descending = false,
			CancellationToken cancellationToken = default)
		{
			var cacheKey = $"Aggregated:{ip}";
			var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
			if (!string.IsNullOrEmpty(cachedJson))
			{
				var cachedResult = JsonSerializer.Deserialize<AggregatedItemDto>(cachedJson);
				if (cachedResult != null) return cachedResult;
			}

			// 1. Get location from IP
			var location = await _geoClient.GetLocationByIpAsync(ip, cancellationToken);
			if (location == null)
				throw new InvalidOperationException("Could not determine location from IP.");

			// 2. Call other APIs in parallel
			var tasks = _providers.ToDictionary(
				p => p.Name,
				p => p.GetDataAsync(location.Latitude, location.Longitude, cancellationToken));

			await Task.WhenAll(tasks.Values);

			// 3. Aggregate results
			var aggregated = new AggregatedItemDto
			{
				City = location.City,
				Country = location.Country,
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				Data = tasks.ToDictionary(t => t.Key, t => t.Value.Result)
			};

			// 4. Apply filtering & sorting for providers implementing IFilterable
			foreach (var key in aggregated.Data.Keys.ToList())
			{
				if (aggregated.Data[key] is IEnumerable<IFilterable> items)
				{
					aggregated.Data[key] = items
						.FilterAndSort(
							filter: x => category == null || x.Category == category,
							sortBy: x => x.CreatedAt,
							descending: true
						)
						.ToList();
				}
			}

			// 5. Cache the aggregated result
			var serialized = JsonSerializer.Serialize(aggregated);
			var cacheOptions = new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
			};
			await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);

			return aggregated;
		}
	}
}
