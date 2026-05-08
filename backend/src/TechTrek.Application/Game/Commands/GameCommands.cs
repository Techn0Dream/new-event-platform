using MediatR;
using System.Text.Json;
using TechTrek.Domain.Entities;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Interfaces;
using TechTrek.Shared.Common;
using TechTrek.Shared.Constants;

namespace TechTrek.Application.Game.Commands;

// ============================================================
// VERIFY RIDDLE COMMAND
// Maps to: POST /api/v1/game/team/{teamId}/verify-riddle
// Called by: VolunteerDashboard → handleVerify()
// VOLUNTEER only. Unlocks LOCKED → ACTIVE for team.
// ============================================================

public sealed record VerifyRiddleCommand(
    Guid TeamId,
    Guid EventId,
    int StageIndex,
    Guid VolunteerId
) : IRequest<Result<VerifyRiddleResult>>;

public sealed record VerifyRiddleResult(
    Guid TeamId,
    int StageIndex,
    string NewStatus,
    string Message
);

public sealed class VerifyRiddleCommandHandler : IRequestHandler<VerifyRiddleCommand, Result<VerifyRiddleResult>>
{
    private readonly ITeamRepository _teams;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IOutboxRepository _outbox;
    private readonly IFeatureFlagRepository _flags;

    public VerifyRiddleCommandHandler(
        ITeamRepository teams,
        IUnitOfWork uow,
        IAuditLogRepository auditLogs,
        IOutboxRepository outbox,
        IFeatureFlagRepository flags)
    {
        _teams = teams;
        _uow = uow;
        _auditLogs = auditLogs;
        _outbox = outbox;
        _flags = flags;
    }

    public async Task<Result<VerifyRiddleResult>> Handle(VerifyRiddleCommand request, CancellationToken cancellationToken)
    {
        // Feature flag check: if event is paused, block
        if (await _flags.IsEnabledAsync(FeatureFlags.PauseEvent, request.EventId, cancellationToken))
            throw new FeatureDisabledException("pause_event");

        var team = await _teams.GetByIdWithProgressAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException("Team", request.TeamId);

        if (team.EventId != request.EventId)
            throw new UnauthorizedException("Team does not belong to this event.");

        // Domain method enforces all invariants
        team.UnlockCurrentStage(request.StageIndex, request.VolunteerId);

        // Write outbox message for SignalR broadcast (Transactional Outbox)
        var outboxPayload = JsonSerializer.Serialize(new
        {
            teamId = request.TeamId,
            eventId = request.EventId,
            stageIndex = request.StageIndex,
            type = "stage_unlocked"
        });

        await _outbox.AddAsync(
            OutboxMessage.Create("StageUnlocked", outboxPayload),
            cancellationToken);

        // Audit log
        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.RiddleVerified, "Team",
            actorId: request.VolunteerId,
            entityId: request.TeamId,
            payloadJson: JsonSerializer.Serialize(new { stageIndex = request.StageIndex }),
            isSuccess: true), cancellationToken);

        // All saved atomically in one transaction
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<VerifyRiddleResult>.Success(new VerifyRiddleResult(
            request.TeamId,
            request.StageIndex,
            "ACTIVE",
            "Stage unlocked. Team can now see and solve the question."));
    }
}

// ============================================================
// SUBMIT ANSWER COMMAND
// Maps to: POST /api/v1/game/team/{teamId}/submit
// Called by: ParticipantDashboard → handleAnswerSubmit()
// PARTICIPANT only. Submits code, advances stage, awards points.
// ============================================================

public sealed record SubmitAnswerCommand(
    Guid TeamId,
    Guid EventId,
    int StageIndex,
    string SubmittedCode,
    Guid ParticipantId
) : IRequest<Result<SubmitAnswerResult>>;

public sealed record SubmitAnswerResult(
    bool Success,
    int PointsAwarded,
    int NewTotalScore,
    int NextStageIndex,
    bool IsGameComplete,
    string Message
);

public sealed class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand, Result<SubmitAnswerResult>>
{
    private readonly ITeamRepository _teams;
    private readonly IEventRepository _events;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IOutboxRepository _outbox;
    private readonly IFeatureFlagRepository _flags;

    public SubmitAnswerCommandHandler(
        ITeamRepository teams,
        IEventRepository events,
        IUnitOfWork uow,
        IAuditLogRepository auditLogs,
        IOutboxRepository outbox,
        IFeatureFlagRepository flags)
    {
        _teams = teams;
        _events = events;
        _uow = uow;
        _auditLogs = auditLogs;
        _outbox = outbox;
        _flags = flags;
    }

    public async Task<Result<SubmitAnswerResult>> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        // Anti-cheat: basic validation
        if (string.IsNullOrWhiteSpace(request.SubmittedCode) || request.SubmittedCode.Length < 10)
            return Result<SubmitAnswerResult>.Failure("Submission is too short to be valid.", "INVALID_SUBMISSION");

        if (await _flags.IsEnabledAsync(FeatureFlags.FreezeSubmissions, request.EventId, cancellationToken))
            throw new FeatureDisabledException(FeatureFlags.FreezeSubmissions);

        var team = await _teams.GetByIdWithProgressAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException("Team", request.TeamId);

        var eventWithStages = await _events.GetByIdWithStagesAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var currentStage = eventWithStages.GetStage(request.StageIndex);

        // Get points from question
        // (In production: would also run through code judge)
        var questionId = currentStage.QuestionId;

        // Domain enforces: stage must be ACTIVE, index must match CurrentStageIndex
        team.CompleteCurrentStage(
            request.StageIndex,
            pointsAwarded: 100, // TODO: fetch from question entity
            submittedCode: request.SubmittedCode,
            userId: request.ParticipantId);

        // Check if team finished all stages
        bool isComplete = team.HasFinished ||
            team.CurrentStageIndex >= eventWithStages.TotalStages;

        if (isComplete && !team.HasFinished)
            team.MarkFinished();

        // Outbox: notify leaderboard + participant via SignalR
        var outboxPayload = JsonSerializer.Serialize(new
        {
            teamId = request.TeamId,
            eventId = request.EventId,
            stageIndex = request.StageIndex,
            newScore = team.Score.Value,
            type = "score_updated"
        });
        await _outbox.AddAsync(OutboxMessage.Create("ScoreUpdated", outboxPayload), cancellationToken);

        // Audit log
        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.AnswerSubmitted, "Team",
            actorId: request.ParticipantId,
            entityId: request.TeamId,
            payloadJson: JsonSerializer.Serialize(new { stageIndex = request.StageIndex, codeLength = request.SubmittedCode.Length }),
            isSuccess: true), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<SubmitAnswerResult>.Success(new SubmitAnswerResult(
            Success: true,
            PointsAwarded: 100, // TODO: fetch actual
            NewTotalScore: team.Score.Value,
            NextStageIndex: team.CurrentStageIndex,
            IsGameComplete: isComplete,
            Message: isComplete ? "🎉 All stages complete! Mission accomplished!" : "Stage complete! Moving to next mission."));
    }
}

// ============================================================
// SKIP QUESTION COMMAND
// Maps to: POST /api/v1/game/team/{teamId}/skip
// Called by: VolunteerDashboard → handleSkip() (emergency override)
// VOLUNTEER only.
// ============================================================

public sealed record SkipQuestionCommand(
    Guid TeamId,
    Guid EventId,
    int StageIndex,
    Guid VolunteerId
) : IRequest<Result<SkipQuestionResult>>;

public sealed record SkipQuestionResult(
    Guid TeamId,
    int SkippedStageIndex,
    int NextStageIndex,
    string Message
);

public sealed class SkipQuestionCommandHandler : IRequestHandler<SkipQuestionCommand, Result<SkipQuestionResult>>
{
    private readonly ITeamRepository _teams;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IOutboxRepository _outbox;

    public SkipQuestionCommandHandler(
        ITeamRepository teams,
        IUnitOfWork uow,
        IAuditLogRepository auditLogs,
        IOutboxRepository outbox)
    {
        _teams = teams;
        _uow = uow;
        _auditLogs = auditLogs;
        _outbox = outbox;
    }

    public async Task<Result<SkipQuestionResult>> Handle(SkipQuestionCommand request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetByIdWithProgressAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException("Team", request.TeamId);

        team.SkipCurrentStage(request.StageIndex, request.VolunteerId);

        var outboxPayload = JsonSerializer.Serialize(new
        {
            teamId = request.TeamId,
            eventId = request.EventId,
            stageIndex = request.StageIndex,
            type = "stage_skipped"
        });
        await _outbox.AddAsync(OutboxMessage.Create("StageSkipped", outboxPayload), cancellationToken);

        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.QuestionSkipped, "Team",
            actorId: request.VolunteerId,
            entityId: request.TeamId,
            payloadJson: JsonSerializer.Serialize(new { stageIndex = request.StageIndex })), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<SkipQuestionResult>.Success(new SkipQuestionResult(
            request.TeamId,
            request.StageIndex,
            team.CurrentStageIndex,
            "Question skipped. Team moved to next stage."));
    }
}

// ============================================================
// ADMIN RESET COMMAND
// Maps to: POST /api/v1/admin/events/{eventId}/reset
// Called by: AdminDashboard → handleResetGame() (Factory Reset)
// ADMIN only.
// ============================================================

public sealed record ResetEventCommand(
    Guid EventId,
    Guid AdminId
) : IRequest<Result>;

public sealed class ResetEventCommandHandler : IRequestHandler<ResetEventCommand, Result>
{
    private readonly ITeamRepository _teams;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogRepository _auditLogs;

    public ResetEventCommandHandler(
        ITeamRepository teams,
        IUnitOfWork uow,
        IAuditLogRepository auditLogs)
    {
        _teams = teams;
        _uow = uow;
        _auditLogs = auditLogs;
    }

    public async Task<Result> Handle(ResetEventCommand request, CancellationToken cancellationToken)
    {
        var teams = await _teams.GetByEventIdAsync(request.EventId, cancellationToken);

        // Reset all team progress (soft reset - data preserved in audit)
        // In production: implement reset logic via domain methods
        // For now: mark for reset via event sourcing replay

        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.EventReset, "Event",
            actorId: request.AdminId,
            entityId: request.EventId,
            payloadJson: JsonSerializer.Serialize(new { teamsReset = teams.Count })), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
