namespace APIAggregator.API.Features.AirQuality
{
	/// <summary>
	/// Configuration options for the Air Quality API.
	/// </summary>
	public class AirQualityApiOptions
	{
		public string ApiKey { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = string.Empty;
	}
}