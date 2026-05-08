using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Entities;
using TechTrek.Domain.Enums;

namespace TechTrek.Domain.Interfaces;

/// <summary>
/// IUserRepository - aggregate-focused, NOT generic.
/// Contains ONLY queries relevant to the User aggregate and its auth workflow.
/// Leaderboard queries live in ILeaderboardRepository.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByUsernameWithTokensAsync(string username, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
    void Remove(User user);
}

/// <summary>
/// IRefreshTokenRepository - manages refresh token lifecycle.
/// Separate from IUserRepository because token queries are security-critical
/// and have different performance characteristics.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IList<RefreshToken>> GetFamilyAsync(string familyId, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    void Update(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default);
    Task RevokeEntireFamilyAsync(string familyId, string reason, CancellationToken ct = default);
}

/// <summary>
/// IEmailTokenRepository - manages email verification and password reset tokens.
/// </summary>
public interface IEmailTokenRepository
{
    Task<EmailVerificationToken?> GetVerificationTokenAsync(string tokenHash, CancellationToken ct = default);
    Task<PasswordResetToken?> GetResetTokenAsync(string tokenHash, CancellationToken ct = default);
    Task AddVerificationTokenAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task AddResetTokenAsync(PasswordResetToken token, CancellationToken ct = default);
    void UpdateVerificationToken(EmailVerificationToken token);
    void UpdateResetToken(PasswordResetToken token);
}

/// <summary>
/// ITeamRepository - aggregate-focused for the Team aggregate root.
/// Includes ALL team queries: game state, membership, progress.
/// All children (TeamMember, StageProgress) are always loaded through this repository.
/// </summary>
public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Team?> GetByIdWithProgressAsync(Guid id, CancellationToken ct = default);
    Task<Team?> GetByIdFullAsync(Guid id, CancellationToken ct = default); // Includes members + progress
    Task<Team?> GetTeamForUserAsync(Guid userId, Guid eventId, CancellationToken ct = default);
    Task<IList<Team>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<bool> NameExistsInEventAsync(string name, Guid eventId, CancellationToken ct = default);
    Task AddAsync(Team team, CancellationToken ct = default);
    void Update(Team team);
}

/// <summary>
/// IEventRepository - aggregate-focused for the Event aggregate root.
/// Includes stage configuration queries.
/// </summary>
public interface IEventRepository
{
    Task<Domain.Aggregates.Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Aggregates.Event?> GetByIdWithStagesAsync(Guid id, CancellationToken ct = default);
    Task<IList<Domain.Aggregates.Event>> GetActiveEventsAsync(CancellationToken ct = default);
    Task<IList<Domain.Aggregates.Event>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Domain.Aggregates.Event evt, CancellationToken ct = default);
    void Update(Domain.Aggregates.Event evt);
}

/// <summary>
/// IGameSessionRepository - responsible for complex game state queries.
/// These are specialized read queries that join Team + Event + Stage data.
/// Uses compiled queries and projections for performance.
/// 
/// Separate from ITeamRepository because game state reads have different
/// loading requirements (full stage details, riddle content, question content).
/// </summary>
public interface IGameSessionRepository
{
    /// <summary>
    /// Returns the full game state for a team.
    /// Maps to frontend: GameEngine.getTeamState(teamId)
    /// </summary>
    Task<GameStateProjection?> GetTeamGameStateAsync(Guid teamId, Guid eventId, CancellationToken ct = default);

    /// <summary>Returns volunteer game state (includes riddle answer for verification).</summary>
    Task<VolunteerGameStateProjection?> GetVolunteerGameStateAsync(Guid teamId, Guid eventId, CancellationToken ct = default);
}

/// <summary>
/// ILeaderboardRepository - specialized for leaderboard queries.
/// Kept separate because leaderboard queries are read-heavy, cached heavily,
/// and have completely different query patterns than other repositories.
/// 
/// These queries NEVER compute from scratch when cached. They use Redis projections
/// invalidated only when QuestionCompletedEvent fires.
/// </summary>
public interface ILeaderboardRepository
{
    Task<IList<LeaderboardEntry>> GetLeaderboardAsync(Guid eventId, int limit = 100, CancellationToken ct = default);
    Task<int?> GetTeamRankAsync(Guid teamId, Guid eventId, CancellationToken ct = default);
}

/// <summary>
/// IVolunteerAssignmentRepository - manages volunteer-to-team assignments.
/// </summary>
public interface IVolunteerAssignmentRepository
{
    Task<VolunteerAssignment?> GetAssignmentAsync(Guid volunteerId, Guid eventId, CancellationToken ct = default);
    Task AddAsync(VolunteerAssignment assignment, CancellationToken ct = default);
    void Update(VolunteerAssignment assignment);
}

/// <summary>
/// IAuditLogRepository - append-only log storage.
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<IList<AuditLog>> GetRecentAsync(Guid eventId, int limit = 50, CancellationToken ct = default);
    Task<IList<AuditLog>> GetForActorAsync(Guid actorId, int limit = 20, CancellationToken ct = default);
}

/// <summary>
/// IOutboxRepository - for the Transactional Outbox Pattern.
/// </summary>
public interface IOutboxRepository
{
    Task<IList<OutboxMessage>> GetPendingAsync(int batchSize = 20, CancellationToken ct = default);
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    void Update(OutboxMessage message);
}

/// <summary>
/// IFeatureFlagRepository - feature flag checks during request processing.
/// </summary>
public interface IFeatureFlagRepository
{
    Task<bool> IsEnabledAsync(string key, Guid? eventId = null, CancellationToken ct = default);
    Task<FeatureFlag?> GetByKeyAsync(string key, Guid? eventId = null, CancellationToken ct = default);
    Task<IList<FeatureFlag>> GetAllAsync(Guid? eventId = null, CancellationToken ct = default);
    Task AddAsync(FeatureFlag flag, CancellationToken ct = default);
    void Update(FeatureFlag flag);
}

/// <summary>
/// IAnnouncementRepository
/// </summary>
public interface IAnnouncementRepository
{
    Task<IList<Announcement>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<IList<Announcement>> GetGlobalAsync(CancellationToken ct = default);
    Task<IList<Announcement>> GetScheduledReadyAsync(CancellationToken ct = default);
    Task AddAsync(Announcement announcement, CancellationToken ct = default);
    void Update(Announcement announcement);
}

// ============================================================
// PROJECTION MODELS (read-only, never saved back)
// ============================================================

/// <summary>
/// Projection of game state for a PARTICIPANT.
/// Maps to: frontend GameEngine.getTeamState() response.
/// </summary>
public sealed record GameStateProjection(
    Guid TeamId,
    string TeamName,
    int Score,
    int CurrentStageIndex,
    int TotalStages,
    string StageStatus,      // "LOCKED" | "ACTIVE" | "COMPLETED"
    RiddleProjection? Riddle,
    QuestionProjection? Question
);

public sealed record RiddleProjection(
    Guid Id,
    string QuestionText,
    string Location
    // Answer NOT included here - only in VolunteerGameStateProjection
);

public sealed record QuestionProjection(
    Guid Id,
    string Title,
    string Description,
    string InputFormat,
    string OutputFormat,
    int Points,
    int TimeLimitSeconds
);

/// <summary>
/// Projection for VOLUNTEER - includes riddle answer for verification.
/// </summary>
public sealed record VolunteerGameStateProjection(
    Guid TeamId,
    string TeamName,
    int CurrentStageIndex,
    string StageStatus,
    RiddleWithAnswerProjection? Riddle
);

public sealed record RiddleWithAnswerProjection(
    Guid Id,
    string QuestionText,
    string Answer,   // Decrypted for volunteer view only
    string Location
);

/// <summary>
/// Leaderboard entry projection.
/// Maps to frontend AdminDashboard leaderboard table.
/// </summary>
public sealed record LeaderboardEntry(
    int Rank,
    Guid TeamId,
    string TeamName,
    int Score,
    int CurrentStage,
    int TotalStages,
    bool HasFinished,
    DateTime LastActivity
);
