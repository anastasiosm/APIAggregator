using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace APIAggregator.API.Infrastructure.Services;

public class ClientIpAddressProvider : IClientIpAddressProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClientIpAddressProvider> _logger;
    
    // Fallback IP for development/testing (Google's public DNS)
    private const string FallbackIp = "8.8.8.8";

    public ClientIpAddressProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ClientIpAddressProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, returning fallback IP");
            return FallbackIp;
        }

        // 1. Check X-Forwarded-For header (from proxy/load balancer)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            _logger.LogDebug("IP from X-Forwarded-For: {Ip}", ip);
            return ip;
        }

        // 2. Check X-Real-IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            _logger.LogDebug("IP from X-Real-IP: {Ip}", realIp);
            return realIp;
        }

        // 3. Get from connection
        var connectionIp = httpContext.Connection.RemoteIpAddress?.ToString();

        // 4. Handle localhost and Docker internal IPs
        if (IsLocalOrDockerIp(connectionIp))
        {
            _logger.LogDebug("Detected local/Docker IP: {Ip}, using fallback: {FallbackIp}", connectionIp, FallbackIp);
            return FallbackIp;
        }

        return connectionIp ?? "unknown";
    }

    private static bool IsLocalOrDockerIp(string? ip)
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