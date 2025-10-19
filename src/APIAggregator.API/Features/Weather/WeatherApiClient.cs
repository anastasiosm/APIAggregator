using APIAggregator.API.Interfaces;

namespace APIAggregator.API.Features.Weather
{
	/// <summary>
	/// Client interface for fetching weather data from an external API.
	/// </summary>
	public class WeatherApiClient : ILocationDataProvider
	{
		private readonly HttpClient _client;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		public string Name => "Weather";

		public WeatherApiClient(HttpClient client, IConfiguration configuration)
		{
			_client = client;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:OpenWeatherMap:ApiKey"]
				?? throw new InvalidOperationException("OpenWeatherMap API key missing.");
		}

		public async Task<object> GetDataAsync(double lat, double lon, CancellationToken ct)
		{
			var result = await GetWeatherAsync(lat, lon, ct);
			return result ?? new WeatherDto(Summary: "No data", TemperatureC: 0, Description: "No data available");
		}

		public async Task<WeatherDto?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";

			try
			{
				var resp = await _client.GetFromJsonAsync<WeatherApiResponse>(url, cancellationToken);
				if (resp?.Weather is { Length: > 0 })
				{
					return new WeatherDto(
						Summary: resp.Weather[0].Main,
						Description: resp.Weather[0].Description,
						TemperatureC: resp.Main.Temp
					);
				}
			}
			catch (HttpRequestException ex)
			{
				// TODO: Log error (Serilog, ILogger, etc.)
				Console.WriteLine($"[WeatherApiClient] Error fetching weather: {ex.Message}");
			}

			return null;
		}

		#region Helper Classes for JSON Deserialization

		/// <summary>
		/// Represents the response from a weather API.
		/// Is being used to deserialize the JSON response from the external API.
		/// </summary>
		/// <remarks>This class is designed to facilitate JSON deserialization of weather API responses.  It includes
		/// information about current weather conditions and key atmospheric metrics.</remarks>
		private class WeatherApiResponse
		{
			public WeatherInfo[] Weather { get; set; } = Array.Empty<WeatherInfo>();
			public MainInfo Main { get; set; } = new();
		}

		private class WeatherInfo
		{
			public string Main { get; set; } = "";
			public string Description { get; set; } = "";
		}

		private class MainInfo
		{
			public double Temp { get; set; }
		}
		#endregion
	}
}
