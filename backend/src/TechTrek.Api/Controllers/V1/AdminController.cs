using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTrek.Application.Game.Commands;
using TechTrek.Application.Game.Queries;
using TechTrek.Shared.Common;
using TechTrek.Shared.Constants;

namespace TechTrek.Api.Controllers.V1;

/// <summary>
/// AdminController - Administrative operations.
/// 
/// Maps to AdminDashboard.jsx flows:
///   GET  /admin/dashboard/{eventId}     → stats + leaderboard + system log
///   POST /admin/events/{eventId}/start  → start event timer
///   POST /admin/events/{eventId}/reset  → handleResetGame() "Factory Reset"
///   GET  /admin/leaderboard/{eventId}   → live rankings table
/// 
/// ADMIN + SUPERADMIN only.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminController : TechTrekBaseController
{
    public AdminController(IMediator mediator) : base(mediator) { }

    /// <summary>GET /api/v1/admin/dashboard/{eventId}</summary>
    [HttpGet("dashboard/{eventId:guid}")]
    public async Task<IActionResult> GetDashboard([FromRoute] Guid eventId, CancellationToken ct)
    {
        var query = new GetAdminDashboardQuery(eventId);
        var result = await Mediator.Send(query, ct);
        return OkResult(result);
    }

    /// <summary>POST /api/v1/admin/events/{eventId}/reset</summary>
    [HttpPost("events/{eventId:guid}/reset")]
    public async Task<IActionResult> ResetEvent([FromRoute] Guid eventId, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());
        var command = new ResetEventCommand(eventId, adminId);
        var result = await Mediator.Send(command, ct);
        return OkResult(result, "Event has been reset.");
    }

    /// <summary>GET /api/v1/admin/leaderboard/{eventId}</summary>
    [HttpGet("leaderboard/{eventId:guid}")]
    public async Task<IActionResult> GetLeaderboard([FromRoute] Guid eventId, CancellationToken ct)
    {
        var query = new GetLeaderboardQuery(eventId);
        var result = await Mediator.Send(query, ct);
        return OkResult(result);
    }
}

/// <summary>
/// LeaderboardController - Public leaderboard access.
/// Maps to any participant-viewable leaderboard page.
/// Authenticated but accessible by all roles.
/// </summary>
[Authorize]
public sealed class LeaderboardController : TechTrekBaseController
{
    public LeaderboardController(IMediator mediator) : base(mediator) { }

    /// <summary>GET /api/v1/leaderboard/{eventId}</summary>
    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetLeaderboard([FromRoute] Guid eventId, CancellationToken ct)
    {
        var query = new GetLeaderboardQuery(eventId, Limit: 100);
        var result = await Mediator.Send(query, ct);
        return OkResult(result);
    }
}
