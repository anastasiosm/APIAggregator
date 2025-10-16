using Microsoft.Extensions.Configuration;

namespace APIAggregator.API.Features.ExternalAPIs
{
	public class IpLocationDto
	{
		public string City { get; set; } = string.Empty;
		public string Country { get; set; } = string.Empty;
		public double Latitude { get; set; }
		public double Longitude { get; set; }
	}

	public interface IIpGeolocationClient
	{
		Task<IpLocationDto?> GetLocationByIpAsync(string ip, CancellationToken cancellationToken);
	}

	public class IpGeolocationClient : IIpGeolocationClient
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		private const string BASE_URL = "https://api.ipstack.com/";

		public IpGeolocationClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClientFactory = httpClientFactory;
			_configuration = configuration;
			_apiKey = _configuration["ExternalAPIs:IPStack:ApiKey"]
				?? throw new InvalidOperationException("IPStack API key missing.");
		}

		public async Task<IpLocationDto?> GetLocationByIpAsync(string ip, CancellationToken cancellationToken)
		{
			var url = $"{BASE_URL}{ip}?access_key={_apiKey}";
			var client = _httpClientFactory.CreateClient();

			try
			{
				var resp = await client.GetFromJsonAsync<IpApiResponse>(url, cancellationToken);
				if (resp != null
					&& !string.IsNullOrEmpty(resp.City)
					&& !string.IsNullOrEmpty(resp.Country_Name)
					&& resp.Latitude != 0 && resp.Longitude != 0)
				{
					return new IpLocationDto
					{
						City = resp.City ?? "",
						Country = resp.Country_Name ?? "",
						Latitude = resp.Latitude,
						Longitude = resp.Longitude
					};
				}
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"[IpGeolocationClient] Error fetching location: {ex.Message}");
			}

			return null;
		}

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
