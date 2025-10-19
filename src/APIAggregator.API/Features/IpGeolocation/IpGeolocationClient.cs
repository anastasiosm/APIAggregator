namespace APIAggregator.API.Features.IpGeolocation
{
	/// <summary>
	/// Provides functionality to retrieve geolocation information for a given IP address using the IPStack API.
	/// </summary>
	/// <remarks>This client communicates with the IPStack API to fetch geolocation data, such as city, country,
	/// latitude, and longitude, for a specified IP address. An API key must be configured in the application settings
	/// under the key <ExternalAPIs:IPStack:ApiKey>.</remarks>
	public class IpGeolocationClient : IIpGeolocationClient
	{
		private readonly HttpClient _client;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		private const string BASE_URL = "https://api.ipstack.com/";

		public IpGeolocationClient(HttpClient client, IConfiguration configuration)
		{
			_client = client;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:IPStack:ApiKey"]
				?? throw new InvalidOperationException("IPStack API key missing.");
		}

		public async Task<IpLocationDto?> GetLocationByIpAsync(string ip, CancellationToken cancellationToken)
		{
			var url = $"{BASE_URL}{ip}?access_key={_apiKey}";

			try
			{
				var resp = await _client.GetFromJsonAsync<IpApiResponse>(url, cancellationToken);
				if (resp != null
					&& !string.IsNullOrEmpty(resp.City)
					&& !string.IsNullOrEmpty(resp.Country_Name)
					&& resp.Latitude != 0 && resp.Longitude != 0)
				{
					return new IpLocationDto(
						resp.City ?? "",
						resp.Country_Name ?? "",
						resp.Latitude,
						resp.Longitude
					);
				}
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"[IpGeolocationClient] Error fetching location: {ex.Message}");
			}

			return null;
		}

		/// <summary>
		/// Represents the response data from an IP geolocation API.
		/// Is being used to deserialize the JSON response from the external API.
		/// </summary>
		/// <remarks>This class contains information about the geographical location of an IP address,  including the
		/// country, region, city, and coordinates. All string properties are nullable  and may be null if the corresponding
		/// data is unavailable.</remarks>
		private class IpApiResponse
		{
			public string? Ip { get; set; }
			public string? Country_Name { get; set; }
			public string? Country_Code { get; set; }
			public string? Region_Name { get; set; }
			public string? Region_Code { get; set; }
			public string? City { get; set; }
			public double Latitude { get; set; }
			public double Longitude { get; set; }
		}
	}
}
