using APIAggregator.API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace APIAggregator.API.Features.Weather
{
	/// <summary>
	/// Client interface for fetching weather data from an external API.
	/// </summary>
	public class WeatherApiClient : ILocationDataProvider
	{
		private readonly HttpClient _client;
		private readonly WeatherApiOptions _options;
		private readonly ILogger<WeatherApiClient> _logger;

		public string Name => "Weather";

		public WeatherApiClient(
			HttpClient client,
			IOptions<WeatherApiOptions> options,
			ILogger<WeatherApiClient> logger)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			if (string.IsNullOrWhiteSpace(_options.ApiKey))
				throw new InvalidOperationException("OpenWeatherMap API key missing.");
		}

		public async Task<object> GetDataAsync(double lat, double lon, CancellationToken ct)
		{
			var result = await GetWeatherAsync(lat, lon, ct);
			return result ?? new WeatherDto(Summary: "No data", TemperatureC: 0, Description: "No data available");
		}

		public async Task<WeatherDto?> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken)
		{
			var url = BuildWeatherUrl(latitude, longitude);

			try
			{
				var resp = await _client.GetFromJsonAsync<WeatherApiResponse>(url, cancellationToken);
				return MapToWeatherDto(resp);
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Error fetching weather data for coordinates: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error fetching weather data for coordinates: Lat={Latitude}, Lon={Longitude}", latitude, longitude);
				return null;
			}
		}

		/// <summary>
		/// Builds the weather API URL with the provided coordinates.
		/// </summary>
		internal virtual string BuildWeatherUrl(double latitude, double longitude)
		{
			return $"{_options.BaseUrl}weather?lat={latitude}&lon={longitude}&appid={_options.ApiKey}&units=metric";
		}

		/// <summary>
		/// Maps the API response to a WeatherDto object.
		/// </summary>
		internal static WeatherDto? MapToWeatherDto(WeatherApiResponse? response)
		{
			if (response?.Weather is { Length: > 0 })
			{
				return new WeatherDto(
					Summary: response.Weather[0].Main,
					Description: response.Weather[0].Description,
					TemperatureC: response.Main.Temp
				);
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
		internal class WeatherApiResponse
		{
			[JsonPropertyName("weather")]
			public WeatherInfo[] Weather { get; set; } = Array.Empty<WeatherInfo>();
			
			[JsonPropertyName("main")]
			public MainInfo Main { get; set; } = new();
		}

		internal class WeatherInfo
		{
			[JsonPropertyName("main")]
			public string Main { get; set; } = "";
			
			[JsonPropertyName("description")]
			public string Description { get; set; } = "";
		}

		internal class MainInfo
		{
			[JsonPropertyName("temp")]
			public double Temp { get; set; }
		}
		#endregion
	}
}
