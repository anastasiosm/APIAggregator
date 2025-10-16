namespace APIAggregator.API.Features.ExternalAPIs
{
	public class WeatherDto
	{
		public string Summary { get; set; } = "";
		public float TemperatureC { get; set; }
	}

	public interface IWeatherApiClient
	{
		Task<WeatherDto?> GetWeatherAsync(float latitude, float longitude, CancellationToken cancellationToken);
	}

	public class WeatherApiClient : IWeatherApiClient
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		public WeatherApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:OpenWeatherMap:ApiKey"] ?? throw new Exception("OpenWeatherMap API key missing.");
		}

		public async Task<WeatherDto?> GetWeatherAsync(float latitude, float longitude, CancellationToken cancellationToken)
		{
			var client = _httpClientFactory.CreateClient();
			var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric";
			var resp = await client.GetFromJsonAsync<WeatherApiResponse>(url, cancellationToken);
			if (resp != null && resp.Weather is { Length: > 0 })
			{
				return new WeatherDto
				{
					Summary = resp.Weather[0].Main,
					TemperatureC = resp.Main.Temp
				};
			}
			return null;
		}

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
			public float Temp { get; set; }
		}
	}
}
