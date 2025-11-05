namespace APIAggregator.API.Infrastructure.Services;

/// <summary>
/// Provides client IP address detection and resolution
/// </summary>
public interface IClientIpAddressProvider
{
    /// <summary>
    /// Gets the client's IP address from the current HTTP context
    /// </summary>
    /// <returns>Client IP address or a fallback value for local/development environments</returns>
    string GetClientIpAddress();
}