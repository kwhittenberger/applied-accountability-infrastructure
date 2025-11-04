using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AppliedAccountability.Security.Authentication;

/// <summary>
/// Default implementation of JWT token service
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly ILogger<JwtTokenService> _logger;
    private readonly JwtTokenOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(
        ILogger<JwtTokenService> logger,
        JwtTokenOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new ArgumentException("JWT secret key cannot be empty", nameof(options));
        }

        _tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.UTF8.GetBytes(_options.SecretKey);
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature);
    }

    /// <inheritdoc />
    public string GenerateToken(
        string userId,
        IEnumerable<Claim>? claims = null,
        TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var tokenExpiration = expiration ?? _options.DefaultExpiration;
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (claims != null)
        {
            tokenClaims.AddRange(claims);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(tokenClaims),
            Expires = DateTime.UtcNow.Add(tokenExpiration),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = _tokenHandler.WriteToken(token);

        _logger.LogDebug(
            "Generated JWT token for user: {UserId}, Expires: {Expiration}",
            userId, tokenDescriptor.Expires);

        return tokenString;
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            var key = Encoding.UTF8.GetBytes(_options.SecretKey);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = _options.ClockSkew
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Invalid JWT token algorithm");
                return null;
            }

            _logger.LogDebug("Successfully validated JWT token");
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Failed to validate JWT token");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetUserId(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var refreshToken = Convert.ToBase64String(randomBytes);

        _logger.LogDebug("Generated refresh token");

        return refreshToken;
    }
}
