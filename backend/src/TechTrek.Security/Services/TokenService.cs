using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TechTrek.Domain.Aggregates;
using TechTrek.Domain.ValueObjects;
using TechTrek.Security.Interfaces;
using TechTrek.Shared.Constants;

namespace TechTrek.Security.Services;

/// <summary>
/// JWT Token Service
/// 
/// Access Token:
/// - HS256 signed with secret key
/// - 15 minute expiry (short-lived for security)
/// - Contains: uid, username, role, tid (teamId), atid (assignedTeamId), eid (eventId), jti (unique token ID), tfam (family)
/// - jti is used for Redis revocation blocklist
/// 
/// Refresh Token:
/// - Cryptographically random 64-byte token
/// - Never stored in plaintext - only SHA-256 hash in DB
/// - 7 day expiry
/// - Token family tracking prevents reuse attacks
/// - Sent as HttpOnly Secure SameSite=Strict cookie
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateAccessToken(User user, Guid? teamId = null, Guid? assignedTeamId = null, Guid? eventId = null)
    {
        var jti = Guid.NewGuid().ToString();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(TechTrek.Shared.Constants.ClaimTypes.UserId, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Username),
            new(TechTrek.Shared.Constants.ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        };

        if (teamId.HasValue)
            claims.Add(new Claim(TechTrek.Shared.Constants.ClaimTypes.TeamId, teamId.Value.ToString()));

        if (assignedTeamId.HasValue)
            claims.Add(new Claim(TechTrek.Shared.Constants.ClaimTypes.AssignedTeamId, assignedTeamId.Value.ToString()));

        if (eventId.HasValue)
            claims.Add(new Claim(TechTrek.Shared.Constants.ClaimTypes.EventId, eventId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return _handler.WriteToken(token);
    }

    public RefreshTokenPair GenerateRefreshToken(Guid userId, string? deviceFingerprint, string? ipAddress, string? userAgent)
    {
        // Cryptographically secure random bytes for the refresh token
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var plainToken = Convert.ToBase64String(randomBytes);

        var tokenHash = TokenHash.Of(plainToken).Value;
        var familyId = Guid.NewGuid().ToString("N"); // New family for each fresh login
        var expiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);

        return new RefreshTokenPair(plainToken, tokenHash, familyId, expiresAt);
    }

    public ClaimsPrincipalResult? ValidateAccessToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expired tokens
            };

            var principal = _handler.ValidateToken(token, parameters, out _);

            var userId = Guid.Parse(principal.FindFirst(TechTrek.Shared.Constants.ClaimTypes.UserId)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value!);

            var role = principal.FindFirst(TechTrek.Shared.Constants.ClaimTypes.Role)?.Value!;
            var username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value!;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var family = principal.FindFirst(TechTrek.Shared.Constants.ClaimTypes.TokenFamily)?.Value;

            Guid? teamId = null;
            var teamIdStr = principal.FindFirst(TechTrek.Shared.Constants.ClaimTypes.TeamId)?.Value;
            if (!string.IsNullOrEmpty(teamIdStr)) teamId = Guid.Parse(teamIdStr);

            Guid? assignedTeamId = null;
            var assignedStr = principal.FindFirst(TechTrek.Shared.Constants.ClaimTypes.AssignedTeamId)?.Value;
            if (!string.IsNullOrEmpty(assignedStr)) assignedTeamId = Guid.Parse(assignedStr);

            return new ClaimsPrincipalResult(userId, username, role, teamId, assignedTeamId, jti, family);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>JWT configuration - loaded from appsettings.json → JwtSettings section.</summary>
public sealed class JwtSettings
{
    public string Secret { get; init; } = default!;
    public string Issuer { get; init; } = "TechTrek";
    public string Audience { get; init; } = "TechTrek-Users";
    public int AccessTokenExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 7;
}
