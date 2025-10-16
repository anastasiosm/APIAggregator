namespace APIAggregator.API.Features.Aggregation
{
	public interface IAggregationService
	{
		Task<AggregatedItemDto> GetAggregatedData(string ip, string? category = null, string? sortBy = null, bool descending = false, CancellationToken cancellationToken = default);
	}
}
