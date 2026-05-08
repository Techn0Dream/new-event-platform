using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechTrek.Domain.Interfaces;
using TechTrek.Realtime.Hubs;
using TechTrek.Shared.Constants;

namespace TechTrek.Infrastructure.BackgroundServices;

/// <summary>
/// OutboxDispatcher - Implements the Transactional Outbox Pattern.
/// 
/// HOW IT WORKS:
/// 1. Domain operations write events to the outbox table IN THE SAME DB TRANSACTION
///    as the business data change. This guarantees atomicity.
/// 2. This background worker polls the outbox table every 1 second.
/// 3. For each unprocessed message, it dispatches to the appropriate destination
///    (SignalR hub, email service, n8n webhook, etc.)
/// 4. On success: marks message as processed.
/// 5. On failure: records error + increments retry count. Retries up to 3 times.
///    After 3 failures: message is stuck in outbox for manual inspection.
/// 
/// WHY THIS IS CORRECT:
/// - If DB commit succeeds but SignalR broadcast fails: message stays in outbox, retry publishes it.
/// - If DB commit fails: message was never written, so no orphan events.
/// - Guarantees: AT-LEAST-ONCE delivery (handlers must be idempotent).
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher encountered an error.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxDispatcher stopped.");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var gameHub = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();
        var leaderboardHub = scope.ServiceProvider.GetRequiredService<IHubContext<LeaderboardHub>>();
        var adminHub = scope.ServiceProvider.GetRequiredService<IHubContext<AdminHub>>();

        var messages = await outbox.GetPendingAsync(batchSize: 20, ct);

        foreach (var message in messages)
        {
            try
            {
                await DispatchMessageAsync(message, gameHub, leaderboardHub, adminHub, ct);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch outbox message {MessageId} of type {EventType}",
                    message.Id, message.EventType);
                message.MarkFailed(ex.Message);
            }

            outbox.Update(message);
        }

        if (messages.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
        }
    }

    private async Task DispatchMessageAsync(
        Domain.Entities.OutboxMessage message,
        IHubContext<GameHub> gameHub,
        IHubContext<LeaderboardHub> leaderboardHub,
        IHubContext<AdminHub> adminHub,
        CancellationToken ct)
    {
        var payload = JsonDocument.Parse(message.PayloadJson);

        switch (message.EventType)
        {
            case "StageUnlocked":
                {
                    // Extract team ID from payload
                    var teamId = payload.RootElement.GetProperty("teamId").GetGuid();
                    var stageIndex = payload.RootElement.GetProperty("stageIndex").GetInt32();

                    await gameHub.Clients
                        .Group(SignalRGroups.TeamGame(teamId))
                        .SendAsync("StageUnlocked", new
                        {
                            teamId,
                            stageIndex,
                            message = "Riddle verified! Your challenge is now accessible.",
                            timestamp = DateTime.UtcNow
                        }, ct);

                    break;
                }

            case "ScoreUpdated":
                {
                    var teamId = payload.RootElement.GetProperty("teamId").GetGuid();
                    var eventId = payload.RootElement.GetProperty("eventId").GetGuid();
                    var newScore = payload.RootElement.GetProperty("newScore").GetInt32();

                    // Notify the team
                    await gameHub.Clients
                        .Group(SignalRGroups.TeamGame(teamId))
                        .SendAsync("GameStateUpdated", new
                        {
                            teamId,
                            newScore,
                            type = "score_updated",
                            timestamp = DateTime.UtcNow
                        }, ct);

                    // Notify leaderboard watchers (admin + public)
                    await leaderboardHub.Clients
                        .Group(SignalRGroups.EventLeaderboard(eventId))
                        .SendAsync("LeaderboardUpdated", new
                        {
                            eventId,
                            teamId,
                            newScore,
                            timestamp = DateTime.UtcNow
                        }, ct);

                    // Notify admins in the system log feed
                    await adminHub.Clients
                        .Group(SignalRGroups.AdminEvent(eventId))
                        .SendAsync("SystemLogEntry", new
                        {
                            teamId,
                            action = "SCORE_UPDATED",
                            score = newScore,
                            timestamp = DateTime.UtcNow
                        }, ct);

                    break;
                }

            case "StageSkipped":
                {
                    var teamId = payload.RootElement.GetProperty("teamId").GetGuid();
                    await gameHub.Clients
                        .Group(SignalRGroups.TeamGame(teamId))
                        .SendAsync("GameStateUpdated", new
                        {
                            teamId,
                            type = "stage_skipped",
                            timestamp = DateTime.UtcNow
                        }, ct);
                    break;
                }

            default:
                _logger.LogWarning("Unknown outbox event type: {EventType}", message.EventType);
                break;
        }
    }
}
