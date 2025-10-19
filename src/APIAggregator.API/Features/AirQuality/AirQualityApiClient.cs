using APIAggregator.API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APIAggregator.API.Features.AirQuality
{
	/// <summary>
	/// Provides functionality to retrieve air quality data for a specified geographic location using the OpenWeatherMap
	/// Air Pollution API.
	/// </summary>
	public class AirQualityApiClient : ILocationDataProvider
	{
		private readonly HttpClient _client;
		private readonly AirQualityApiOptions _options;
		private readonly ILogger<AirQualityApiClient> _logger;

		public string Name => "AirQuality";

		// Use IOptions<> instead of IConfiguration
		public AirQualityApiClient(
			HttpClient client, 
			IOptions<AirQualityApiOptions> options,
			ILogger<AirQualityApiClient> logger)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			if (string.IsNullOrWhiteSpace(_options.ApiKey))
				throw new InvalidOperationException("OpenWeatherMap API key missing.");
		}

		public async Task<object> GetDataAsync(double lat, double lon, CancellationToken ct)
		{
			var result = await GetAirQualityAsync(lat, lon, ct);
			return result ?? new AirQualityDto(AQI: 0, PM25: 0, PM10: 0);
		}

		public async Task<AirQualityDto?> GetAirQualityAsync(double lat, double lon, CancellationToken cancellationToken)
		{
			var url = BuildAirQualityUrl(lat, lon);

			try
			{
				var resp = await _client.GetFromJsonAsync<AirQualityApiResponse>(url, cancellationToken);
				return MapToAirQualityDto(resp);
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Error fetching air quality data for coordinates: Lat={Latitude}, Lon={Longitude}", lat, lon);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error fetching air quality data for coordinates: Lat={Latitude}, Lon={Longitude}", lat, lon);
				return null;
			}
		}

		/// <summary>
		/// Builds the air quality API URL with the provided coordinates.
		/// </summary>
		internal virtual string BuildAirQualityUrl(double latitude, double longitude)
		{
			return $"{_options.BaseUrl}air_pollution?lat={latitude}&lon={longitude}&appid={_options.ApiKey}";
		}

		/// <summary>
		/// Maps the API response to an AirQualityDto object.
		/// </summary>
		internal static AirQualityDto? MapToAirQualityDto(AirQualityApiResponse? response)
		{
			if (response?.List is { Length: > 0 })
			{
				var main = response.List[0].Main;
				var components = response.List[0].Components;
				return new AirQualityDto(					
					AQI: main.Aqi,
					PM25: components.Pm25,
					PM10: components.Pm10
				);
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
		internal class AirQualityApiResponse
		{
			public AirQualityRecord[] List { get; set; } = Array.Empty<AirQualityRecord>();
		}

		internal class AirQualityRecord
		{
			public MainInfo Main { get; set; } = new();
			public ComponentsInfo Components { get; set; } = new();
		}

		internal class MainInfo
		{
			public int Aqi { get; set; }
		}

		internal class ComponentsInfo
		{
			public double Pm25 { get; set; }
			public double Pm10 { get; set; }
		}
		#endregion
	}
}
