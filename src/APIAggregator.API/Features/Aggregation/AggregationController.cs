using APIAggregator.API.Features.Aggregation;
using APIAggregator.API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AggregationController : ControllerBase
{
    private readonly IAggregationService _aggregationService;
    private readonly IClientIpAddressProvider _ipAddressProvider;
    private readonly ILogger<AggregationController> _logger;

    public AggregationController(
        IAggregationService aggregationService,
        IClientIpAddressProvider ipAddressProvider,
        ILogger<AggregationController> logger)
    {
        _aggregationService = aggregationService;
        _ipAddressProvider = ipAddressProvider;
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
        // If IP not provided, detect from request
        if (string.IsNullOrWhiteSpace(ip))
        {
            ip = _ipAddressProvider.GetClientIpAddress();
        }

        // Validate
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
}