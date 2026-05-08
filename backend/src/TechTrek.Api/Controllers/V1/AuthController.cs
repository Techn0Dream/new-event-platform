using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTrek.Application.Auth.Commands;
using TechTrek.Shared.Common;

namespace TechTrek.Api.Controllers.V1;

/// <summary>
/// AuthController - Handles all authentication flows.
/// 
/// Maps directly to frontend authService.js:
/// - Register: /api/v1/auth/register
/// - Login: /api/v1/auth/login
/// - Refresh: /api/v1/auth/refresh
/// - Logout: /api/v1/auth/logout
/// - Me: /api/v1/auth/me (current user info)
/// 
/// Security:
/// - Refresh token delivered as HttpOnly Secure SameSite=Strict cookie
/// - Access token returned in response body (stored in memory by frontend, NOT localStorage)
/// - Rate limited (via IP + fingerprint) on login endpoint
/// </summary>
[AllowAnonymous]
public sealed class AuthController : TechTrekBaseController
{
    public AuthController(IMediator mediator) : base(mediator) { }

    /// <summary>
    /// POST /api/v1/auth/register
    /// Register new participant. Step 1 of 2-step register flow.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterCommand(
            request.Username,
            request.DisplayName,
            request.Email,
            request.Password,
            request.IsTeam,
            request.TeamName,
            request.TeammateEmail);

        var result = await Mediator.Send(command, ct);
        return OkResult(result, "Registration successful!");
    }

    /// <summary>
    /// POST /api/v1/auth/login
    /// Login and receive access + refresh token.
    /// Frontend maps this to: role → redirect to /participant, /volunteer, or /admin
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password,
            request.DeviceFingerprint,
            GetClientIp(),
            GetUserAgent());

        var result = await Mediator.Send(command, ct);
        if (result.IsFailure) return OkResult(result);

        // Set refresh token as HttpOnly cookie
        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        // Return access token + user info in body (frontend stores in memory)
        return OkResult(result, "Login successful.");
    }

    /// <summary>
    /// POST /api/v1/auth/refresh
    /// Rotate access token using refresh token from HttpOnly cookie.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        // Get refresh token from HttpOnly cookie (preferred) or body
        var refreshToken = Request.Cookies["refresh_token"]
            ?? request?.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token not found." });

        var command = new RefreshTokenCommand(
            refreshToken,
            request?.DeviceFingerprint,
            GetClientIp());

        var result = await Mediator.Send(command, ct);
        if (result.IsFailure) return OkResult(result);

        SetRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);
        return OkResult(result, "Token refreshed.");
    }

    /// <summary>
    /// POST /api/v1/auth/logout
    /// Revokes the current token family.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refresh_token"] ?? string.Empty;

        var userId = Guid.Parse(User.FindFirst(Shared.Constants.ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());

        var command = new LogoutCommand(userId, refreshToken);
        await Mediator.Send(command, ct);

        // Clear the cookie
        Response.Cookies.Delete("refresh_token");
        return OkResult(Result.Success(), "Logged out successfully.");
    }

    /// <summary>
    /// GET /api/v1/auth/me
    /// Returns current user info from JWT claims.
    /// Called by frontend on app init to restore session.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(Shared.Constants.ClaimTypes.UserId)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(Shared.Constants.ClaimTypes.Role)?.Value;
        var teamId = User.FindFirst(Shared.Constants.ClaimTypes.TeamId)?.Value;

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Current user info",
            Data = new { userId, username, role, teamId },
            Timestamp = DateTime.UtcNow,
            TraceId = TraceId
        });
    }

    // ============================================================
    // Private Helpers
    // ============================================================

    private void SetRefreshTokenCookie(string token, DateTime expiry)
    {
        Response.Cookies.Append("refresh_token", token, new CookieOptions
        {
            HttpOnly = true,      // JS cannot read this cookie (XSS protection)
            Secure = true,        // HTTPS only
            SameSite = SameSiteMode.Strict, // CSRF protection
            Expires = expiry,
            Path = "/api/v1/auth" // Scoped - only sent to auth endpoints
        });
    }
}

// ============================================================
// Request DTOs - These are what the frontend sends
// ============================================================

public sealed record RegisterRequest(
    string Username,
    string DisplayName,
    string Email,
    string Password,
    bool IsTeam,
    string? TeamName,
    string? TeammateEmail
);

public sealed record LoginRequest(
    string Username,
    string Password,
    string? DeviceFingerprint
);

public sealed record RefreshRequest(
    string? RefreshToken,
    string? DeviceFingerprint
);
