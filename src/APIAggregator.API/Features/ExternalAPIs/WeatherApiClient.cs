using System.Net.Http;

namespace APIAggregator.API.Features.ExternalAPIs
{
	public class WeatherDto
	{
		public string Summary { get; set; } = "";
		public double TemperatureC { get; set; }
		public string Description { get; set; } = "";
	}	

	public class WeatherApiClient : ILocationDataProvider
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		public string Name => "Weather";

		public WeatherApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] 
				?? throw new InvalidOperationException("OpenWeatherMap API key missing.");
		}

		public async Task<object> GetDataAsync(double lat, double lon, CancellationToken ct)
		{ 
			var result = await GetWeatherAsync(lat, lon, ct); 
			return result ?? new WeatherDto { Summary = "No data", TemperatureC = 0 };
		}

		public async Task<WeatherDto?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";
			var client = _httpClientFactory.CreateClient();

			try
			{
				var resp = await client.GetFromJsonAsync<WeatherApiResponse>(url, cancellationToken);
				if (resp?.Weather is { Length: > 0 })
				{
					return new WeatherDto
					{
						Summary = resp.Weather[0].Main,
						Description = resp.Weather[0].Description,
						TemperatureC = resp.Main.Temp
					};
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
