namespace APIAggregator.API.Features.Statistics;

/// <summary>
/// Provides methods for retrieving and recording API statistics.
/// </summary>
/// <remarks>This service interface allows for the collection and retrieval of statistical data related to API
/// requests.</remarks>
public interface IStatisticsService
{
	/// <summary>
	/// Records a single API request metric.
	/// </summary>
	/// <remarks>This method is used to log the performance of API calls by recording the time taken for each
	/// request.  Ensure that <paramref name="apiName"/> is a valid identifier for the API being monitored.</remarks>
	/// <param name="apiName">The name of the API for which the request duration is being recorded. Cannot be null or empty.</param>
	/// <param name="totalMilliseconds">The total time, in milliseconds, that the API request took to complete. Must be non-negative.</param>
	void RecordRequest(string apiName, double totalMilliseconds);

	/// <summary>
	/// Retrieves aggregated statistics for all APIs
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<StatisticsDto> GetStatistics(CancellationToken cancellationToken = default);
}