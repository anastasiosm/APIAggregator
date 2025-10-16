using APIAggregator.API.Features.ExternalAPIs;

namespace APIAggregator.API.Features.Aggregation
{
	public class AggregationService : IAggregationService
	{
		private readonly IIpGeolocationClient _geoClient;
		private readonly IEnumerable<ILocationDataProvider> _providers;

		public AggregationService(IIpGeolocationClient geoClient, IEnumerable<ILocationDataProvider> providers)
		{
			_geoClient = geoClient;
			_providers = providers;
		}

		public async Task<AggregatedItemDto> GetAggregatedData(string ip, CancellationToken cancellationToken)
		{
			// 1. Get location from IP
			var location = await _geoClient.GetLocationByIpAsync(ip, cancellationToken);
			if (location == null)
				throw new Exception("Could not determine location from IP.");

			// 2. Call other APIs in parallel
			var tasks = _providers.ToDictionary(
				p => p.Name,
				p => p.GetDataAsync(location.Latitude, location.Longitude, cancellationToken));

			await Task.WhenAll(tasks.Values);

			return new AggregatedItemDto
			{
				City = location.City,
				Country = location.Country,
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				Data = tasks.ToDictionary(t => t.Key, t => t.Value.Result)
			};
		}
	}
}
