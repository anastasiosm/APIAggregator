/// <summary>
/// HTTP INTERCEPTOR
/// </summary>
using APIAggregator.API.Features.Statistics;
using System.Diagnostics;

namespace APIAggregator.API.Infrastructure.Http;

/*
## 🔧 Handler Order
 Request Flow:
─────────────
1. StatisticsTrackingHandler  ⏱️ (Start timing - OUTERMOST)
2. Retry Policy               🔁
3. Timeout Policy             ⏰
4. Circuit Breaker            🔌
5. HttpClientHandler          🌐 (Actual HTTP call)
   ↓
   Response
   ↓
5. HttpClientHandler          
4. Circuit Breaker            
3. Timeout Policy             
2. Retry Policy               
1. StatisticsTrackingHandler  ⏱️ (Stop timing - measures TOTAL time)
 * */

/// <summary>
/// HTTP message handler that tracks request duration and records statistics
/// </summary>
public class StatisticsTrackingHandler : DelegatingHandler
{
	private readonly IStatisticsService _statisticsService;
	private readonly string _apiName;
	private readonly ILogger<StatisticsTrackingHandler>? _logger;

	/// <summary>
	/// Initializes a new instance of the StatisticsTrackingHandler
	/// </summary>
	/// <param name="statisticsService">Service for recording statistics</param>
	/// <param name="apiName">Name of the API being tracked (e.g., "IPStack", "Weather")</param>
	/// <param name="logger">Optional logger for debugging</param>
	public StatisticsTrackingHandler(
		IStatisticsService statisticsService,
		string apiName,
		ILogger<StatisticsTrackingHandler>? logger = null)
	{
		_statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
		_apiName = apiName ?? throw new ArgumentNullException(nameof(apiName));
		_logger = logger;
	}

	/// <summary>
	/// Sends an HTTP request and records timing statistics
	/// </summary>
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		// Start timing before the request
		var stopwatch = Stopwatch.StartNew();

		_logger?.LogDebug(
			"[{ApiName}] Starting request to {RequestUri}",
			_apiName,
			request.RequestUri);

		HttpResponseMessage? response = null;
		try
		{
			// Call the next handler in the pipeline (which eventually calls the external API)
			response = await base.SendAsync(request, cancellationToken);

			// Stop timing after successful response
			stopwatch.Stop();

			// Record the metric
			_statisticsService.RecordRequest(_apiName, stopwatch.Elapsed.TotalMilliseconds);

			_logger?.LogDebug(
				"[{ApiName}] Request completed in {Duration}ms with status {StatusCode}",
				_apiName,
				stopwatch.ElapsedMilliseconds,
				(int)response.StatusCode);

			return response;
		}
		catch (Exception ex)
		{
			// Stop timing even on error
			stopwatch.Stop();

			// Record the metric (including failed requests)
			_statisticsService.RecordRequest(_apiName, stopwatch.Elapsed.TotalMilliseconds);

			_logger?.LogWarning(
				ex,
				"[{ApiName}] Request failed after {Duration}ms",
				_apiName,
				stopwatch.ElapsedMilliseconds);

			// Re-throw the exception to maintain normal error handling flow
			throw;
		}
	}
}

///// <summary>
///// Extension methods for registering StatisticsTrackingHandler
///// **** Simplifies adding the handler to HttpClient builders.****
///// </summary>
//public static class StatisticsTrackingHandlerExtensions
//{
//	/// <summary>
//	/// Adds statistics tracking to an HTTP client builder
//	/// </summary>
//	/// <param name="builder">The HTTP client builder</param>
//	/// <param name="apiName">Name of the API for statistics tracking</param>
//	/// <returns>The HTTP client builder for chaining</returns>
//	public static IHttpClientBuilder AddStatisticsTracking(
//		this IHttpClientBuilder builder,
//		string apiName)
//	{
//		return builder.AddHttpMessageHandler(sp =>
//		{
//			var statsService = sp.GetRequiredService<IStatisticsService>();
//			var logger = sp.GetService<ILogger<StatisticsTrackingHandler>>();
//			return new StatisticsTrackingHandler(statsService, apiName, logger);
//		});
//	}
//}