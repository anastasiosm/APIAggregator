namespace APIAggregator.API.Features.Aggregation
{
	public interface IAggregationService
	{
		Task<AggregatedItemDto> GetAggregatedData(string ip, CancellationToken cancellationToken);
	}
}
