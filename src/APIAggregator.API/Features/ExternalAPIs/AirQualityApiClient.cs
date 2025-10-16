namespace APIAggregator.API.Features.ExternalAPIs
{
	public class AirQualityDto
	{
		public int AQI { get; set; }
		public float PM25 { get; set; }
		public float PM10 { get; set; }
	}

	public interface IAirQualityApiClient
	{
		Task<AirQualityDto?> GetAirQualityAsync(float lat, float lon, CancellationToken cancellationToken);
	}

	public class AirQualityApiClient : IAirQualityApiClient
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		public AirQualityApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] ?? throw new Exception("OpenWeatherMap API key missing.");
		}

		public async Task<AirQualityDto?> GetAirQualityAsync(float lat, float lon, CancellationToken cancellationToken)
		{
			var client = _httpClientFactory.CreateClient();
			var url = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={lat}&lon={lon}&appid={_apiKey}";
			var resp = await client.GetFromJsonAsync<AirQualityApiResponse>(url, cancellationToken);
			if (resp?.List is { Length: > 0 })
			{
				var main = resp.List[0].Main;
				var components = resp.List[0].Components;
				return new AirQualityDto
				{
					AQI = main.Aqi,
					PM25 = components.Pm25,
					PM10 = components.Pm10
				};
			}
			return null;
		}

		private class AirQualityApiResponse
		{
			public AirQualityRecord[] List { get; set; } = Array.Empty<AirQualityRecord>();
		}

		private class AirQualityRecord
		{
			public MainInfo Main { get; set; } = new();
			public ComponentsInfo Components { get; set; } = new();
		}

		private class MainInfo
		{
			public int Aqi { get; set; }
		}

		private class ComponentsInfo
		{
			public float Pm25 { get; set; }
			public float Pm10 { get; set; }
		}
	}
}
