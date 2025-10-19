namespace APIAggregator.API.Features.ExternalAPIs
{
	/// <summary>
	/// Defines a contract for retrieving location-based data.
	/// </summary>
	/// <remarks>Implementations of this interface provide location-specific data based on geographic
	/// coordinates.</remarks>
	public interface ILocationDataProvider
	{
		/// <summary>
		/// Gets the name associated with the current instance of <see cref="ILocationDataProvider"/>."/>
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Asynchronously retrieves data for the specified geographic coordinates.
		/// </summary>
		/// <param name="latitude">The latitude of the location for which data is requested. Must be in the range -90 to 90.</param>
		/// <param name="longitude">The longitude of the location for which data is requested. Must be in the range -180 to 180.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will terminate early if the token is canceled.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an object with the data for the
		/// specified location.</returns>
		Task<object> GetDataAsync(double latitude, double longitude, CancellationToken cancellationToken);
	}
}
