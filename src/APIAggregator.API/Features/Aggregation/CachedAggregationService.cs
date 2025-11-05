using APIAggregator.API.Infrastructure.Caching;

namespace APIAggregator.API.Features.Aggregation;

/// <summary>
/// Decorator that adds caching capabilities to IAggregationService implementations.
/// </summary>
public class CachedAggregationService : IAggregationService
{
	private readonly IAggregationService _innerService;
	private readonly IDistributedCacheService _cacheService;
	private readonly ILogger<CachedAggregationService> _logger;
	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

	public CachedAggregationService(
		IAggregationService innerService,
		IDistributedCacheService cacheService,
		ILogger<CachedAggregationService> logger)
	{
		_innerService = innerService;
		_cacheService = cacheService;
		_logger = logger;
	}

	public async Task<AggregatedItemDto> GetAggregatedData(
		string ip,
		string? category = null,
		string? sortBy = null,
		bool descending = false,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"Aggregated:{ip}";

		// 1. Try cache first
		var cachedResult = await _cacheService.GetAsync<AggregatedItemDto>(cacheKey, cancellationToken);
		if (cachedResult != null)
		{
			_logger.LogInformation("Returning cached result for {Ip}", ip);
			return cachedResult;
		}

		// 2. Delegate to inner service
		var aggregated = await _innerService.GetAggregatedData(ip, category, sortBy, descending, cancellationToken);

		// 3. Cache the result
		await _cacheService.SetAsync(cacheKey, aggregated, CacheDuration, cancellationToken);

		return aggregated;
	}
}



