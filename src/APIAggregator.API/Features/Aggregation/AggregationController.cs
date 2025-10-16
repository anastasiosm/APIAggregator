using Microsoft.AspNetCore.Mvc;

namespace APIAggregator.API.Features.Aggregation
{
	[ApiController]
	[Route("api/[controller]")]
	public class AggregationController : ControllerBase
	{
		private readonly IAggregationService _aggregationService;

		public AggregationController(IAggregationService aggregationService)
		{
			_aggregationService = aggregationService;
		}

		[HttpGet]
		public async Task<ActionResult<AggregatedItemDto>> GetAggregatedData(CancellationToken cancellationToken)
		{
			// Get user's IP address - doesn't work with localhost!!!
			var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
			if (string.IsNullOrEmpty(ip) || ip == "::1" || ip == "127.0.0.1") // localhost: fallback
				ip = "8.8.8.8";
				

			var result = await _aggregationService.GetAggregatedData(ip, cancellationToken);
			return Ok(result);
		}
	}
}
