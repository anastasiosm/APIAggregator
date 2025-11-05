using APIAggregator.API.Extensions;
using APIAggregator.API.Features.IpGeolocation;
using APIAggregator.API.Interfaces;

namespace APIAggregator.API.Features.Aggregation
{
	public class AggregationService : IAggregationService
	{
		private readonly IIpGeolocationClient _geoClient;
		private readonly IEnumerable<ILocationDataProvider> _providers;
		private readonly ILogger<AggregationService> _logger;

		public AggregationService(
			IIpGeolocationClient geoClient,
			IEnumerable<ILocationDataProvider> providers,
			ILogger<AggregationService> logger)
		{
			_geoClient = geoClient;
			_providers = providers;
			_logger = logger;
		}

		public async Task<AggregatedItemDto> GetAggregatedData(
			string ip,
			string? category = null,
			string? sortBy = null,
			bool descending = false,
			CancellationToken cancellationToken = default)
		{
			// 1. Get location from IP
			var location = await _geoClient.GetLocationByIpAsync(ip, cancellationToken)
				?? throw new InvalidOperationException("Could not determine location from IP.");

			// 2. Call all providers in parallel
			var tasks = _providers.ToDictionary(
				p => p.Name,
				p => p.GetDataAsync(location.Latitude, location.Longitude, cancellationToken));

			await Task.WhenAll(tasks.Values);

			// 3. Aggregate results
			var results = tasks.ToDictionary(
				task => task.Key,
				task => (object?)task.Value.Result
			);

			var aggregated = new AggregatedItemDto(
				City: location.City,
				Country: location.Country,
				Latitude: location.Latitude,
				Longitude: location.Longitude,
				Data: results
			);

			// 4. Apply filtering & sorting
			aggregated.ApplyFilteringAndSorting(category);

			return aggregated;
		}
	}
}
