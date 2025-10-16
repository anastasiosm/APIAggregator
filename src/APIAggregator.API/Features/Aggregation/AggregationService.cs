using APIAggregator.API.Features.ExternalAPIs;

namespace APIAggregator.API.Features.Aggregation
{

	public class AggregationService : IAggregationService
	{
		private readonly IIpGeolocationClient _geoClient;
		private readonly IWeatherApiClient _weatherClient;
		private readonly IAirQualityApiClient _airQualityClient;

		public AggregationService(
			IIpGeolocationClient geoClient,
			IWeatherApiClient weatherClient,
			IAirQualityApiClient airQualityClient)
		{
			_geoClient = geoClient;
			_weatherClient = weatherClient;
			_airQualityClient = airQualityClient;
		}

		public async Task<AggregatedItemDto> GetAggregatedData(string ip, CancellationToken cancellationToken)
		{
			// 1. Get location from IP
			var location = await _geoClient.GetLocationByIpAsync(ip, cancellationToken);
			if (location == null)
				throw new Exception("Could not determine location from IP.");

			// 2. In parallel: fetch weather & air quality
			var weatherTask = _weatherClient.GetWeatherAsync(location.Latitude, location.Longitude, cancellationToken);
			var airQualityTask = _airQualityClient.GetAirQualityAsync(location.Latitude, location.Longitude, cancellationToken);

			await Task.WhenAll(weatherTask, airQualityTask);

			return new AggregatedItemDto
			{
				City = location.City,
				Country = location.Country,
				Latitude = location.Latitude,
				Longitude = location.Longitude,
				Weather = weatherTask.Result,
				AirQuality = airQualityTask.Result
			};
		}
	}
}
