namespace APIAggregator.API.Features.ExternalAPIs
{
	public class AirQualityDto
	{
		public int AQI { get; set; }
		public float PM25 { get; set; }
		public float PM10 { get; set; }
	}	

	public class AirQualityApiClient : ILocationDataProvider
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;
		public string Name => "AirQuality";

		public AirQualityApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] 
				?? throw new InvalidOperationException("OpenWeatherMap API key missing.");
		}

		public async Task<object?> GetDataAsync(float lat, float lon, CancellationToken ct)
		{
			var result = await GetAirQualityAsync(lat, lon, ct);
			return result;
		}

		public async Task<AirQualityDto?> GetAirQualityAsync(float lat, float lon, CancellationToken cancellationToken)
		{
			var url = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={lat}&lon={lon}&appid={_apiKey}";
			var client = _httpClientFactory.CreateClient();

			try
			{
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
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"[AirQualityApiClient] Error fetching air quality: {ex.Message}");
			}

			return null;
		}
		

		#region Helper Classes for JSON Deserialization
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
		#endregion
	}
}
