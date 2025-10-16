namespace APIAggregator.API.Features.ExternalAPIs
{
	public interface ILocationDataProvider
	{
		string Name { get; }
		Task<object> GetDataAsync(float latitude, float longitude, CancellationToken cancellationToken);
	}
}
