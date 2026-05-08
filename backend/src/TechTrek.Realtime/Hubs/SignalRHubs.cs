using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TechTrek.Shared.Constants;

namespace TechTrek.Realtime.Hubs;

/// <summary>
/// GameHub - Real-time game state updates.
/// 
/// Replaces frontend's setInterval(refreshGameState, 5000):
/// - Participants join group "team:{teamId}:game"
/// - Volunteers join same group
/// - Server pushes updates when volunteer verifies riddle or participant submits
/// 
/// Client events:
///   - "GameStateUpdated" → participant receives new stage state
///   - "StageUnlocked" → participant sees question appear
///   - "AnswerSubmitted" → volunteer sees team moved to next stage
///   - "TeamFinished" → completion celebration
/// 
/// Authentication: JWT Bearer via SignalR's built-in auth
/// </summary>
[Authorize]
public sealed class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;

    public GameHub(ILogger<GameHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var teamId = Context.User?.FindFirst(ClaimTypes.TeamId)?.Value
            ?? Context.User?.FindFirst(ClaimTypes.AssignedTeamId)?.Value;

        if (!string.IsNullOrEmpty(teamId))
        {
            var groupName = SignalRGroups.TeamGame(Guid.Parse(teamId));
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} joined group {Group}", Context.ConnectionId, groupName);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client calls this to manually request their current game state.</summary>
    public async Task RequestGameState(string teamId)
    {
        await Clients.Caller.SendAsync("GameStateRequested", new { teamId, timestamp = DateTime.UtcNow });
    }
}

/// <summary>
/// LeaderboardHub - Real-time leaderboard updates.
/// 
/// Replaces AdminDashboard's setInterval(loadData, 5000).
/// 
/// Groups:
///   - "event:{eventId}:leaderboard" → all admins watching this event
/// 
/// Broadcast triggers:
///   - When QuestionCompletedEvent fires → score changes → leaderboard pushed
/// 
/// This hub also serves the public-facing leaderboard (if authenticated).
/// Authentication: Required for admin group, optional for public view.
/// </summary>
[Authorize]
public sealed class LeaderboardHub : Hub
{
    private readonly ILogger<LeaderboardHub> _logger;

    public LeaderboardHub(ILogger<LeaderboardHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var eventId = Context.User?.FindFirst(ClaimTypes.EventId)?.Value;

        if (!string.IsNullOrEmpty(eventId))
        {
            var groupName = SignalRGroups.EventLeaderboard(Guid.Parse(eventId));
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client joined leaderboard group {Group}", groupName);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinEventLeaderboard(string eventId)
    {
        var groupName = SignalRGroups.EventLeaderboard(Guid.Parse(eventId));
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedLeaderboard", new { eventId, groupName });
    }
}

/// <summary>
/// AdminHub - Admin-specific real-time updates.
/// 
/// Used for:
///   - System log feed (replaces mock static log in AdminDashboard)
///   - Admin notifications (team finished, suspicious activity, etc.)
///   - Global timer broadcasts
/// 
/// Only ADMIN and SUPERADMIN roles can connect.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminHub : Hub
{
    private readonly ILogger<AdminHub> _logger;

    public AdminHub(ILogger<AdminHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var eventId = Context.User?.FindFirst(ClaimTypes.EventId)?.Value;

        if (!string.IsNullOrEmpty(eventId))
        {
            var groupName = SignalRGroups.AdminEvent(Guid.Parse(eventId));
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        await base.OnConnectedAsync();
    }
}
