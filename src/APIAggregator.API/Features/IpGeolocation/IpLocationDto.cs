namespace APIAggregator.API.Features.IpGeolocation
{
	/// <summary>
	/// DTO object that represents the geographical location associated with an IP address.
	/// </summary>
	/// <param name="City">The name of the city where the IP address is located. This value may be null or empty if the city is not available.</param>
	/// <param name="Country">The name of the country where the IP address is located. This value may be null or empty if the country is not
	/// available.</param>
	/// <param name="Latitude">The latitude coordinate of the IP address location. Values range from -90.0 to 90.0.</param>
	/// <param name="Longitude">The longitude coordinate of the IP address location. Values range from -180.0 to 180.0.</param>
	public record IpLocationDto(string City, string Country, double Latitude, double Longitude);
}