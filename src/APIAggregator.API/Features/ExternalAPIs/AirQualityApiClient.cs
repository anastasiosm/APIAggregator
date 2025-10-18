namespace APIAggregator.API.Features.ExternalAPIs
{
	public class AirQualityDto
	{
		public int AQI { get; set; }
		public double PM25 { get; set; }
		public double PM10 { get; set; }
	}	

	public class AirQualityApiClient : ILocationDataProvider
	{
		private readonly HttpClient _client;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		public string Name => "AirQuality";

		public AirQualityApiClient(HttpClient client, IConfiguration configuration)
		{
			_client = client;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] 
				?? throw new InvalidOperationException("OpenWeatherMap API key missing.");
		}

		public async Task<object> GetDataAsync(double lat, double lon, CancellationToken ct)
		{
			var result = await GetAirQualityAsync(lat, lon, ct);
			return result ?? new AirQualityDto { AQI = 0, PM25 = 0, PM10 = 0 };
		}

		public async Task<AirQualityDto?> GetAirQualityAsync(double lat, double lon, CancellationToken cancellationToken)
		{
			var url = $"https://api.openweathermap.org/data/2.5/air_pollution?lat={lat}&lon={lon}&appid={_apiKey}";

			try
			{
				var resp = await _client.GetFromJsonAsync<AirQualityApiResponse>(url, cancellationToken);

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
			public double Pm25 { get; set; }
			public double Pm10 { get; set; }
		}
		#endregion
	}
}
