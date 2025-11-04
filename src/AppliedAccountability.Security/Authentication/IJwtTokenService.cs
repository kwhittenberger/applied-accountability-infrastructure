using System.Security.Claims;

namespace AppliedAccountability.Security.Authentication;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token with the specified claims
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="claims">Additional claims to include</param>
    /// <param name="expiration">Token expiration time (default: 1 hour)</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(
        string userId,
        IEnumerable<Claim>? claims = null,
        TimeSpan? expiration = null);

    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts user ID from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    string? GetUserId(string token);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();
}

/// <summary>
/// Options for JWT token configuration
/// </summary>
public class JwtTokenOptions
{
    /// <summary>
    /// Secret key for signing tokens
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = "AppliedAccountability";

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = "AppliedAccountability";

    /// <summary>
    /// Default token expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Refresh token expiration time
    /// </summary>
    public TimeSpan RefreshTokenExpiration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Clock skew for token validation
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
