namespace APIAggregator.API.Features.Weather
{
	/// <summary>
	/// Configuration options for the Weather API.
	/// </summary>
	public class WeatherApiOptions
	{
		public string ApiKey { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = string.Empty;
	}
}