namespace APIAggregator.API.Features.ExternalAPIs
{
	/// <summary>
	/// Provides functionality to retrieve air quality data for a specified geographic location using the OpenWeatherMap
	/// Air Pollution API.
	/// </summary>
	/// <remarks>This client fetches air quality data, including AQI (Air Quality Index), PM2.5, and PM10 levels,
	/// for a given latitude and longitude. The client requires an API key for the OpenWeatherMap service, which must be
	/// configured in the application settings under the key "ExternalAPIs:OpenWeatherMap:ApiKey".</remarks>
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
			return result ?? new AirQualityDto(AQI: 0, PM25: 0, PM10: 0);
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
					return new AirQualityDto(					
						AQI: main.Aqi,
						PM25: components.Pm25,
						PM10: components.Pm10
					);
				}
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"[AirQualityApiClient] Error fetching air quality: {ex.Message}");
			}

			return null;
		}


		#region Helper Classes for JSON Deserialization

		/// <summary>
		/// Represents the response from an air quality API, containing a collection of air quality records.
		/// Is being used to deserialize the JSON response from the external API.
		/// </summary>
		/// <remarks>This class is used for deserializing JSON responses from the air quality API.  The <see
		/// cref="List"/> property contains the data records returned by the API.</remarks>
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
