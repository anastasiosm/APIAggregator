namespace APIAggregator.API.Features.ExternalAPIs
{
	public interface ILocationDataProvider
	{
		string Name { get; }
		Task<object> GetDataAsync(double latitude, double longitude, CancellationToken cancellationToken);
	}
}
