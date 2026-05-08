using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTrek.Application.Game.Commands;
using TechTrek.Application.Game.Queries;
using TechTrek.Shared.Common;
using TechTrek.Shared.Constants;

namespace TechTrek.Api.Controllers.V1;

/// <summary>
/// GameController - Core game mechanics endpoints.
/// 
/// API surface mapped exactly from frontend game flows:
/// 
/// PARTICIPANT routes:
///   GET  /game/team/{teamId}/state         → ParticipantDashboard (replaces 5s poll)
///   POST /game/team/{teamId}/submit         → handleAnswerSubmit()
/// 
/// VOLUNTEER routes:
///   GET  /game/team/{teamId}/volunteer-state → VolunteerDashboard
///   POST /game/team/{teamId}/verify-riddle   → handleVerify()
///   POST /game/team/{teamId}/skip            → handleSkip()
/// </summary>
[Authorize]
public sealed class GameController : TechTrekBaseController
{
    public GameController(IMediator mediator) : base(mediator) { }

    // ============================================================
    // PARTICIPANT ENDPOINTS
    // ============================================================

    /// <summary>GET /api/v1/game/team/{teamId}/state</summary>
    [HttpGet("team/{teamId:guid}/state")]
    [ProducesResponseType(typeof(ApiResponse<Domain.Interfaces.GameStateProjection>), 200)]
    public async Task<IActionResult> GetGameState([FromRoute] Guid teamId, [FromQuery] Guid eventId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());

        var query = new GetTeamGameStateQuery(teamId, eventId, userId);
        var result = await Mediator.Send(query, ct);
        return OkResult(result);
    }

    /// <summary>POST /api/v1/game/team/{teamId}/submit</summary>
    [HttpPost("team/{teamId:guid}/submit")]
    [Authorize(Policy = AuthorizationPolicies.ParticipantOnly)]
    public async Task<IActionResult> SubmitAnswer(
        [FromRoute] Guid teamId,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());
        var userTeamId = Guid.TryParse(User.FindFirst(ClaimTypes.TeamId)?.Value, out var tid) ? tid : (Guid?)null;

        // Authorization: participants can only submit for their own team
        if (userTeamId != teamId)
            return Unauthorized(new { message = "You can only submit for your own team." });

        var command = new SubmitAnswerCommand(
            teamId,
            request.EventId,
            request.StageIndex,
            request.SubmittedCode,
            userId);

        var result = await Mediator.Send(command, ct);
        return OkResult(result);
    }

    // ============================================================
    // VOLUNTEER ENDPOINTS
    // ============================================================

    /// <summary>GET /api/v1/game/team/{teamId}/volunteer-state</summary>
    [HttpGet("team/{teamId:guid}/volunteer-state")]
    [Authorize(Policy = AuthorizationPolicies.VolunteerOnly)]
    public async Task<IActionResult> GetVolunteerGameState([FromRoute] Guid teamId, [FromQuery] Guid eventId, CancellationToken ct)
    {
        var volunteerId = Guid.Parse(User.FindFirst(ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());

        var query = new GetVolunteerGameStateQuery(teamId, eventId, volunteerId);
        var result = await Mediator.Send(query, ct);
        return OkResult(result);
    }

    /// <summary>POST /api/v1/game/team/{teamId}/verify-riddle</summary>
    [HttpPost("team/{teamId:guid}/verify-riddle")]
    [Authorize(Policy = AuthorizationPolicies.VolunteerOnly)]
    public async Task<IActionResult> VerifyRiddle(
        [FromRoute] Guid teamId,
        [FromBody] VerifyRiddleRequest request,
        CancellationToken ct)
    {
        var volunteerId = Guid.Parse(User.FindFirst(ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());
        var assignedTeamId = Guid.TryParse(User.FindFirst(ClaimTypes.AssignedTeamId)?.Value, out var aid) ? aid : (Guid?)null;

        // Authorization: volunteers can only verify for their ASSIGNED team
        if (assignedTeamId is not null && assignedTeamId != teamId)
            return Unauthorized(new { message = "You are only assigned to a different team." });

        var command = new VerifyRiddleCommand(teamId, request.EventId, request.StageIndex, volunteerId);
        var result = await Mediator.Send(command, ct);
        return OkResult(result);
    }

    /// <summary>POST /api/v1/game/team/{teamId}/skip</summary>
    [HttpPost("team/{teamId:guid}/skip")]
    [Authorize(Policy = AuthorizationPolicies.VolunteerOnly)]
    public async Task<IActionResult> SkipQuestion(
        [FromRoute] Guid teamId,
        [FromBody] SkipQuestionRequest request,
        CancellationToken ct)
    {
        var volunteerId = Guid.Parse(User.FindFirst(ClaimTypes.UserId)?.Value ?? Guid.Empty.ToString());

        var command = new SkipQuestionCommand(teamId, request.EventId, request.StageIndex, volunteerId);
        var result = await Mediator.Send(command, ct);
        return OkResult(result);
    }
}

// ============================================================
// Request DTOs
// ============================================================

public sealed record SubmitAnswerRequest(
    Guid EventId,
    int StageIndex,
    string SubmittedCode
);

public sealed record VerifyRiddleRequest(
    Guid EventId,
    int StageIndex
);

public sealed record SkipQuestionRequest(
    Guid EventId,
    int StageIndex
);
