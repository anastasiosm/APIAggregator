using APIAggregator.API.Features.ExternalAPIs;

namespace APIAggregator.API.Features.Aggregation
{
	public class AggregatedItemDto
	{
		public string City { get; set; } = string.Empty;
		public string Country { get; set; } = string.Empty;
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		// The results from various external APIs
		public Dictionary<string, object?> Data { get; set; } = new();
	}
}
