using APIAggregator.API.Features.Aggregation;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AggregationController : ControllerBase
{
	private readonly IAggregationService _aggregationService;
	private readonly ILogger<AggregationController> _logger;

	public AggregationController(
		IAggregationService aggregationService,
		ILogger<AggregationController> logger)
	{
		_aggregationService = aggregationService;
		_logger = logger;
	}

	/// <summary>
	/// Get aggregated data for a location based on IP address
	/// </summary>
	/// <param name="ip">IP address (optional - will auto-detect if not provided)</param>
	/// <param name="category">Filter by category</param>
	/// <param name="sortBy">Sort field</param>
	/// <param name="descending">Sort descending</param>
	/// <param name="cancellationToken">Cancellation token</param>
	[HttpGet]
	[ProducesResponseType(typeof(AggregatedItemDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<AggregatedItemDto>> GetAggregatedData(
		[FromQuery] string? ip = null,
		[FromQuery] string? category = null,
		[FromQuery] string? sortBy = null,
		[FromQuery] bool descending = false,
		CancellationToken cancellationToken = default)
	{
		// If IP not provided, try to detect from request
		if (string.IsNullOrWhiteSpace(ip))
		{
			ip = GetClientIpAddress();
		}

		// Validate and sanitize IP
		if (string.IsNullOrWhiteSpace(ip) || ip == "unknown")
		{
			_logger.LogWarning("Unable to determine client IP address");
			return BadRequest("Unable to determine IP address. Please provide it explicitly using ?ip=YOUR_IP");
		}

		_logger.LogInformation("Processing aggregation request for IP: {Ip}", ip);

		var result = await _aggregationService.GetAggregatedData(
			ip,
			category: category,
			sortBy: sortBy,
			descending: descending,
			cancellationToken: cancellationToken);

		return Ok(result);
	}

	private string GetClientIpAddress()
	{
		// 1. Check X-Forwarded-For header (from proxy/load balancer)
		var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
		if (!string.IsNullOrWhiteSpace(forwardedFor))
		{
			var ip = forwardedFor.Split(',')[0].Trim();
			_logger.LogDebug("IP from X-Forwarded-For: {Ip}", ip);
			return ip;
		}

		// 2. Check X-Real-IP header
		var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
		if (!string.IsNullOrWhiteSpace(realIp))
		{
			_logger.LogDebug("IP from X-Real-IP: {Ip}", realIp);
			return realIp;
		}

		// 3. Get from connection
		var connectionIp = HttpContext.Connection.RemoteIpAddress?.ToString();

		// 4. Handle localhost and Docker internal IPs
		if (IsLocalOrDockerIp(connectionIp))
		{
			_logger.LogDebug("Detected local/Docker IP: {Ip}, using fallback: 8.8.8.8", connectionIp);
			return "8.8.8.8"; // Fallback for development/testing
		}

		return connectionIp ?? "unknown";
	}

	private bool IsLocalOrDockerIp(string? ip)
	{
		if (string.IsNullOrWhiteSpace(ip))
			return true;

		return ip == "::1"
			|| ip == "127.0.0.1"
			|| ip.StartsWith("::ffff:127.")
			|| ip.StartsWith("::ffff:172.")
			|| ip.StartsWith("172.")
			|| ip.StartsWith("192.168.");
	}
}