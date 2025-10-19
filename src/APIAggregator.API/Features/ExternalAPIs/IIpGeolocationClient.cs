namespace APIAggregator.API.Features.ExternalAPIs
{
	/// <summary>
	/// Client interface for fetching IP geolocation data from an external API.
	/// </summary>
	public interface IIpGeolocationClient
	{
		/// <summary>
		/// Retrieves the geographical location associated with the specified IP address.
		/// </summary>
		/// <param name="ip">The IP address for which to retrieve the location. Must be a valid IPv4 or IPv6 address.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IpLocationDto"/>
		/// representing the geographical location of the IP address, or <see langword="null"/> if the location could not be
		/// determined.</returns>
		Task<IpLocationDto?> GetLocationByIpAsync(string ip, CancellationToken cancellationToken);
	}
}