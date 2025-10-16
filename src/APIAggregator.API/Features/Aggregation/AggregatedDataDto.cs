using APIAggregator.API.Features.ExternalAPIs;

namespace APIAggregator.API.Features.Aggregation
{
	public class AggregatedItemDto
	{
		public string City { get; set; } = "";
		public string Country { get; set; } = "";
		public float? Latitude { get; set; }
		public float? Longitude { get; set; }
		public WeatherDto? Weather { get; set; }
		public AirQualityDto? AirQuality { get; set; }
	}
}
