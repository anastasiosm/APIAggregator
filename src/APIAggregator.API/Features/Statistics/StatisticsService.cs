using System.Collections.Concurrent;

namespace APIAggregator.API.Features.Statistics;

/// <summary>
/// Statistics tracking
/// </summary>
public class StatisticsService : IStatisticsService
{
	private readonly ILogger<StatisticsService> _logger;
	// we need a thread-safe collection as this service may be accessed concurrently.
	private readonly ConcurrentBag<RequestMetric> _metrics = new();

	public StatisticsService(ILogger<StatisticsService> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	// TODO: make it async
	public Task<StatisticsDto> GetStatistics(CancellationToken cancellationToken = default)
	{
		// TEST DATA - TEMPORARY
		var stats = new StatisticsDto(new List<ApiStatisticsDto>
			{
				new ApiStatisticsDto(
					"IPStack",
					150,
					245.5,
					new PerformanceBucketsDto(30, 80, 40)
				),
				new ApiStatisticsDto(
					"Weather",
					150,
					180.2,
					new PerformanceBucketsDto(50, 70, 30)
				),
				new ApiStatisticsDto(
					"AirQuality",
					120,
					90.5,
					new PerformanceBucketsDto(75, 35, 10)
				)
			});


		return Task.FromResult(stats);
	}
		
	public void RecordRequest(string apiName, double totalMilliseconds)
	{
		_logger.LogInformation(
			"[{ApiName}] Recorded request with duration {Duration} ms",
			apiName,
			totalMilliseconds);

		_metrics.Add(new RequestMetric
		{
			ApiName = apiName,
			DurationMs = totalMilliseconds,
			Timestamp = DateTime.UtcNow
		});		
	}
}

/// <summary>
/// Represents a single API request metric 
/// </summary>
public class RequestMetric
{
	public string ApiName { get; set; }
	public double DurationMs { get; set; }
	public DateTime Timestamp { get; set; }
}

/*
 * FROM LOGGING:
 * System.Net.Http.HttpClient.IpGeolocationClient.LogicalHandler: Information: Start processing HTTP request GET https://api.ipstack.com/8.8.8.8?access_key=ffcac56cee1e590d9bc630ef84f70625
System.Net.Http.HttpClient.IpGeolocationClient.ClientHandler: Information: Sending HTTP request GET https://api.ipstack.com/8.8.8.8?access_key=ffcac56cee1e590d9bc630ef84f70625
System.Net.Http.HttpClient.IpGeolocationClient.ClientHandler: Information: Received HTTP response headers after 834.9204ms - 200
APIAggregator.API.Features.Statistics.StatisticsService: Information: [IpStack] Recorded request with duration 884.5311 ms
System.Net.Http.HttpClient.IpGeolocationClient.LogicalHandler: Information: End processing HTTP request after 901.4095ms - 200
System.Net.Http.HttpClient.Default.LogicalHandler: Information: Start processing HTTP request GET https://api.openweathermap.org/data/2.5/weather?lat=37.38801956176758&lon=-122.07431030273438&appid=6b81f233893d0de1cb12c42a2ca4ae07&units=metric
System.Net.Http.HttpClient.Default.ClientHandler: Information: Sending HTTP request GET https://api.openweathermap.org/data/2.5/weather?lat=37.38801956176758&lon=-122.07431030273438&appid=6b81f233893d0de1cb12c42a2ca4ae07&units=metric
System.Net.Http.HttpClient.Default.LogicalHandler: Information: Start processing HTTP request GET https://api.openweathermap.org/data/2.5/air_pollution?lat=37.38801956176758&lon=-122.07431030273438&appid=6b81f233893d0de1cb12c42a2ca4ae07
System.Net.Http.HttpClient.Default.ClientHandler: Information: Sending HTTP request GET https://api.openweathermap.org/data/2.5/air_pollution?lat=37.38801956176758&lon=-122.07431030273438&appid=6b81f233893d0de1cb12c42a2ca4ae07
System.Net.Http.HttpClient.Default.ClientHandler: Information: Received HTTP response headers after 213.254ms - 200
System.Net.Http.HttpClient.Default.LogicalHandler: Information: End processing HTTP request after 225.2773ms - 200
System.Net.Http.HttpClient.Default.ClientHandler: Information: Received HTTP response headers after 237.6395ms - 200
System.Net.Http.HttpClient.Default.LogicalHandler: Information: End processing HTTP request after 249.0002ms - 200
 * 
 * */