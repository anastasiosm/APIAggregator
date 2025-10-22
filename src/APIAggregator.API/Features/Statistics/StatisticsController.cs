using Microsoft.AspNetCore.Mvc;

namespace APIAggregator.API.Features.Statistics
{
	/*
	TECHNICAL ANALYSIS 

	Create an API endpoint to retrieve request statstcs. Specifcally, return for each API
	the total number of requests and the average response tme, grouped in
	performance buckets (e.g. fast <100ms, average 100-200ms, slow > 200ms – the
	values are indicatve feel free to tweak). For the purposes of the exercise source data
	can be stored in memory. 

	 Ένα endpoint που επιστρέφει:
		Ανά external API (IPStack, Weather, AirQuality):
		Συνολικό αριθμό requests
		Μέσο χρόνο απόκρισης
		Κατανομή σε performance buckets (fast/average/slow)

	Response format:
	{
	  "statistics": [
		{
		  "apiName": "IPStack",
		  "totalRequests": 150,
		  "averageResponseTime": 245.5,
		  "performanceBuckets": {
			"fast": 30,
			"average": 80,
			"slow": 40
		  }
		},
		{
		  "apiName": "Weather",
		  "totalRequests": 150,
		  "averageResponseTime": 180.2,
		  "performanceBuckets": {
			"fast": 50,
			"average": 70,
			"slow": 30
		  }
		},
		{
		  "apiName": "AirQuality",
		  "totalRequests": 120,
		  "averageResponseTime": 90.5,
		  "performanceBuckets": {
			"fast": 75,
			"average": 35,
			"slow": 10
		  }
		}
	  ]
	}
	 */

	public record StatisticsDto(List<ApiStatisticsDto> Statistics);

	public record ApiStatisticsDto(string ApiName,int TotalRequests,double AverageResponseTime,	PerformanceBucketsDto PerformanceBuckets	);

	public record PerformanceBucketsDto(int Fast,int Average,int Slow);

	

	[ApiController]
	[Route("api/[controller]")]
	public class StatisticsController : ControllerBase
	{
		private readonly IStatisticsService _statisticsService;
		private readonly ILogger<StatisticsController> _logger;
		public StatisticsController(
			IStatisticsService statisticsService,
			ILogger<StatisticsController> logger)
		{
			_statisticsService = statisticsService;
			_logger = logger;
		}
		
		[HttpGet]
		[ProducesResponseType(typeof(StatisticsDto), StatusCodes.Status200OK)]
		public async Task<ActionResult<StatisticsDto>> GetStatistics(
			CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Processing statistics request");
			var stats = await _statisticsService.GetStatistics(cancellationToken);
			return Ok(stats);
		}
	}	
}
