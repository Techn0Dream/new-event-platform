using MediatR;
using TechTrek.Domain.Interfaces;
using TechTrek.Shared.Common;

namespace TechTrek.Application.Game.Queries;

// ============================================================
// GET TEAM GAME STATE QUERY
// Maps to: GET /api/v1/game/team/{teamId}/state
// Called by: ParticipantDashboard (via polling → now SignalR)
// Returns: { status, riddle, question, team, stageIndex }
// ============================================================

public sealed record GetTeamGameStateQuery(
    Guid TeamId,
    Guid EventId,
    Guid RequestingUserId
) : IRequest<Result<GameStateProjection>>;

public sealed class GetTeamGameStateQueryHandler : IRequestHandler<GetTeamGameStateQuery, Result<GameStateProjection>>
{
    private readonly IGameSessionRepository _gameSessions;

    public GetTeamGameStateQueryHandler(IGameSessionRepository gameSessions)
        => _gameSessions = gameSessions;

    public async Task<Result<GameStateProjection>> Handle(GetTeamGameStateQuery request, CancellationToken cancellationToken)
    {
        var gameState = await _gameSessions.GetTeamGameStateAsync(
            request.TeamId, request.EventId, cancellationToken);

        if (gameState is null)
            return Result<GameStateProjection>.NotFound($"Game state not found for team {request.TeamId}.");

        return Result<GameStateProjection>.Success(gameState);
    }
}

// ============================================================
// GET VOLUNTEER GAME STATE QUERY
// Maps to: GET /api/v1/game/team/{teamId}/volunteer-state
// Called by: VolunteerDashboard
// Returns same as participant state BUT includes riddle answer (decrypted)
// ============================================================

public sealed record GetVolunteerGameStateQuery(
    Guid TeamId,
    Guid EventId,
    Guid VolunteerId
) : IRequest<Result<VolunteerGameStateProjection>>;

public sealed class GetVolunteerGameStateQueryHandler : IRequestHandler<GetVolunteerGameStateQuery, Result<VolunteerGameStateProjection>>
{
    private readonly IGameSessionRepository _gameSessions;
    private readonly Security.Interfaces.IEncryptionService _encryption;

    public GetVolunteerGameStateQueryHandler(
        IGameSessionRepository gameSessions,
        Security.Interfaces.IEncryptionService encryption)
    {
        _gameSessions = gameSessions;
        _encryption = encryption;
    }

    public async Task<Result<VolunteerGameStateProjection>> Handle(GetVolunteerGameStateQuery request, CancellationToken cancellationToken)
    {
        var state = await _gameSessions.GetVolunteerGameStateAsync(
            request.TeamId, request.EventId, cancellationToken);

        if (state is null)
            return Result<VolunteerGameStateProjection>.NotFound("Volunteer game state not found.");

        // Decrypt riddle answer only for volunteers
        if (state.Riddle is not null)
        {
            var decryptedAnswer = _encryption.Decrypt(state.Riddle.Answer);
            state = state with
            {
                Riddle = state.Riddle with { Answer = decryptedAnswer }
            };
        }

        return Result<VolunteerGameStateProjection>.Success(state);
    }
}

// ============================================================
// GET LEADERBOARD QUERY
// Maps to: GET /api/v1/leaderboard/{eventId}
// Called by: AdminDashboard live rankings table
// Cached in Redis, invalidated on QuestionCompletedEvent
// ============================================================

public sealed record GetLeaderboardQuery(
    Guid EventId,
    int Limit = 100
) : IRequest<Result<IList<LeaderboardEntry>>>;

public sealed class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, Result<IList<LeaderboardEntry>>>
{
    private readonly ILeaderboardRepository _leaderboard;

    public GetLeaderboardQueryHandler(ILeaderboardRepository leaderboard)
        => _leaderboard = leaderboard;

    public async Task<Result<IList<LeaderboardEntry>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var entries = await _leaderboard.GetLeaderboardAsync(
            request.EventId, request.Limit, cancellationToken);

        return Result<IList<LeaderboardEntry>>.Success(entries);
    }
}

// ============================================================
// GET ADMIN DASHBOARD QUERY
// Maps to: GET /api/v1/admin/dashboard/{eventId}
// Called by: AdminDashboard overview stats (teams count, timer, etc.)
// ============================================================

public sealed record GetAdminDashboardQuery(
    Guid EventId
) : IRequest<Result<AdminDashboardData>>;

public sealed record AdminDashboardData(
    int TotalTeams,
    int ActiveTeams,
    int FinishedTeams,
    int? TimerRemainingSeconds,
    bool IsTimerRunning,
    string EventStatus,
    IList<LeaderboardEntry> Leaderboard,
    IList<RecentActivityItem> RecentActivity
);

public sealed record RecentActivityItem(
    string TeamName,
    string Action,
    string Details,
    DateTime OccurredAt
);

public sealed class GetAdminDashboardQueryHandler : IRequestHandler<GetAdminDashboardQuery, Result<AdminDashboardData>>
{
    private readonly ITeamRepository _teams;
    private readonly IEventRepository _events;
    private readonly ILeaderboardRepository _leaderboard;
    private readonly IAuditLogRepository _auditLogs;

    public GetAdminDashboardQueryHandler(
        ITeamRepository teams,
        IEventRepository events,
        ILeaderboardRepository leaderboard,
        IAuditLogRepository auditLogs)
    {
        _teams = teams;
        _events = events;
        _leaderboard = leaderboard;
        _auditLogs = auditLogs;
    }

    public async Task<Result<AdminDashboardData>> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var evt = await _events.GetByIdAsync(request.EventId, cancellationToken);
        if (evt is null)
            return Result<AdminDashboardData>.NotFound($"Event {request.EventId} not found.");

        var allTeams = await _teams.GetByEventIdAsync(request.EventId, cancellationToken);
        var leaderboard = await _leaderboard.GetLeaderboardAsync(request.EventId, 50, cancellationToken);
        var recentLogs = await _auditLogs.GetRecentAsync(request.EventId, 20, cancellationToken);

        var recentActivity = recentLogs
            .Select(log => new RecentActivityItem(
                TeamName: "System",
                Action: log.Action,
                Details: log.PayloadJson ?? string.Empty,
                OccurredAt: log.CreatedAt))
            .ToList();

        return Result<AdminDashboardData>.Success(new AdminDashboardData(
            TotalTeams: allTeams.Count,
            ActiveTeams: allTeams.Count(t => !t.HasFinished),
            FinishedTeams: allTeams.Count(t => t.HasFinished),
            TimerRemainingSeconds: evt.RemainingSeconds,
            IsTimerRunning: evt.IsTimerRunning,
            EventStatus: evt.Status.ToString(),
            Leaderboard: leaderboard,
            RecentActivity: recentActivity));
    }
}
