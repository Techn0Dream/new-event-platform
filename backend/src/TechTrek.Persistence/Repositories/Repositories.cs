using Microsoft.EntityFrameworkCore;
using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Entities;
using TechTrek.Domain.Enums;
using TechTrek.Domain.Interfaces;

namespace TechTrek.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);

    public async Task<User?> GetByUsernameWithTokensAsync(string username, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.RefreshTokens.Where(rt => !rt.RevokedAt.HasValue && rt.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(u => u.Username == username.ToLower(), ct);

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
        => await _db.Users.AnyAsync(u => u.Username == username.ToLower(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public void Update(User user) => _db.Users.Update(user);
    public void Remove(User user) => _db.Users.Remove(user);
}

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public async Task<IList<RefreshToken>> GetFamilyAsync(string familyId, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Where(rt => rt.FamilyId == familyId)
            .ToListAsync(ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await _db.RefreshTokens.AddAsync(token, ct);

    public void Update(RefreshToken token) => _db.RefreshTokens.Update(token);

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.RevokedAt.HasValue)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke(reason);
    }

    public async Task RevokeEntireFamilyAsync(string familyId, string reason, CancellationToken ct = default)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.FamilyId == familyId && !rt.RevokedAt.HasValue)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.Revoke(reason);
    }
}

public sealed class EmailTokenRepository : IEmailTokenRepository
{
    private readonly AppDbContext _db;
    public EmailTokenRepository(AppDbContext db) => _db = db;

    public async Task<EmailVerificationToken?> GetVerificationTokenAsync(string tokenHash, CancellationToken ct = default)
        => await _db.EmailVerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<PasswordResetToken?> GetResetTokenAsync(string tokenHash, CancellationToken ct = default)
        => await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddVerificationTokenAsync(EmailVerificationToken token, CancellationToken ct = default)
        => await _db.EmailVerificationTokens.AddAsync(token, ct);

    public async Task AddResetTokenAsync(PasswordResetToken token, CancellationToken ct = default)
        => await _db.PasswordResetTokens.AddAsync(token, ct);

    public void UpdateVerificationToken(EmailVerificationToken token) => _db.EmailVerificationTokens.Update(token);
    public void UpdateResetToken(PasswordResetToken token) => _db.PasswordResetTokens.Update(token);
}

public sealed class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _db;
    public TeamRepository(AppDbContext db) => _db = db;

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Teams.FindAsync(new object[] { id }, ct);

    public async Task<Team?> GetByIdWithProgressAsync(Guid id, CancellationToken ct = default)
        => await _db.Teams
            .Include(t => t.StageProgresses.OrderBy(sp => sp.StageIndex))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Team?> GetByIdFullAsync(Guid id, CancellationToken ct = default)
        => await _db.Teams
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .Include(t => t.StageProgresses.OrderBy(sp => sp.StageIndex))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Team?> GetTeamForUserAsync(Guid userId, Guid eventId, CancellationToken ct = default)
        => await _db.Teams
            .Include(t => t.Members)
            .Include(t => t.StageProgresses)
            .Where(t => t.EventId == eventId && t.Members.Any(m => m.UserId == userId))
            .FirstOrDefaultAsync(ct);

    public async Task<IList<Team>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
        => await _db.Teams
            .Include(t => t.StageProgresses)
            .Where(t => t.EventId == eventId)
            .OrderByDescending(t => t.Score)
            .ToListAsync(ct);

    public async Task<bool> NameExistsInEventAsync(string name, Guid eventId, CancellationToken ct = default)
        => await _db.Teams.AnyAsync(t => t.Name == name && t.EventId == eventId, ct);

    public async Task AddAsync(Team team, CancellationToken ct = default)
        => await _db.Teams.AddAsync(team, ct);

    public void Update(Team team) => _db.Teams.Update(team);
}

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    public EventRepository(AppDbContext db) => _db = db;

    public async Task<Domain.Aggregates.Event?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Events.FindAsync(new object[] { id }, ct);

    public async Task<Domain.Aggregates.Event?> GetByIdWithStagesAsync(Guid id, CancellationToken ct = default)
        => await _db.Events
            .Include(e => e.Stages.OrderBy(s => s.StageIndex))
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IList<Domain.Aggregates.Event>> GetActiveEventsAsync(CancellationToken ct = default)
        => await _db.Events
            .Where(e => e.Status == EventStatus.Active)
            .ToListAsync(ct);

    public async Task<IList<Domain.Aggregates.Event>> GetAllAsync(CancellationToken ct = default)
        => await _db.Events.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Domain.Aggregates.Event evt, CancellationToken ct = default)
        => await _db.Events.AddAsync(evt, ct);

    public void Update(Domain.Aggregates.Event evt) => _db.Events.Update(evt);
}

public sealed class LeaderboardRepository : ILeaderboardRepository
{
    private readonly AppDbContext _db;
    public LeaderboardRepository(AppDbContext db) => _db = db;

    public async Task<IList<LeaderboardEntry>> GetLeaderboardAsync(Guid eventId, int limit = 100, CancellationToken ct = default)
    {
        // Compiled projection query - never loads full entities
        var teams = await _db.Teams
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderByDescending(t => t.Score)
                .ThenBy(t => t.UpdatedAt) // Tiebreaker: first team to reach score wins
            .Take(limit)
            .Select(t => new
            {
                t.Id,
                t.Name,
                Score = t.Score.Value,
                t.CurrentStageIndex,
                t.HasFinished,
                t.UpdatedAt
            })
            .ToListAsync(ct);

        // Count total stages for the event
        var totalStages = await _db.EventStages.AsNoTracking()
            .Where(es => es.EventId == eventId)
            .CountAsync(ct);

        return teams.Select((t, index) => new LeaderboardEntry(
            Rank: index + 1,
            TeamId: t.Id,
            TeamName: t.Name,
            Score: t.Score,
            CurrentStage: t.CurrentStageIndex,
            TotalStages: totalStages,
            HasFinished: t.HasFinished,
            LastActivity: t.UpdatedAt
        )).ToList();
    }

    public async Task<int?> GetTeamRankAsync(Guid teamId, Guid eventId, CancellationToken ct = default)
    {
        var team = await _db.Teams.AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new { Score = t.Score.Value })
            .FirstOrDefaultAsync(ct);

        if (team is null) return null;

        var rank = await _db.Teams.AsNoTracking()
            .Where(t => t.EventId == eventId && t.Score.Value > team.Score)
            .CountAsync(ct);

        return rank + 1;
    }
}

public sealed class GameSessionRepository : IGameSessionRepository
{
    private readonly AppDbContext _db;
    public GameSessionRepository(AppDbContext db) => _db = db;

    public async Task<GameStateProjection?> GetTeamGameStateAsync(Guid teamId, Guid eventId, CancellationToken ct = default)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .Include(t => t.StageProgresses.OrderBy(sp => sp.StageIndex))
            .FirstOrDefaultAsync(t => t.Id == teamId && t.EventId == eventId, ct);

        if (team is null) return null;

        var eventWithStages = await _db.Events
            .AsNoTracking()
            .Include(e => e.Stages.OrderBy(s => s.StageIndex))
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (eventWithStages is null) return null;

        var currentStageIndex = team.CurrentStageIndex;
        var totalStages = eventWithStages.Stages.Count;
        var isFinished = team.HasFinished || currentStageIndex >= totalStages;

        if (isFinished)
        {
            return new GameStateProjection(
                team.Id, team.Name, team.Score.Value, currentStageIndex, totalStages,
                "COMPLETED", null, null);
        }

        var currentStage = eventWithStages.Stages.FirstOrDefault(s => s.StageIndex == currentStageIndex);
        if (currentStage is null) return null;

        var stageStatus = team.GetStageStatus(currentStageIndex);
        var statusStr = stageStatus == Domain.Enums.StageStatus.Active ? "ACTIVE" : "LOCKED";

        // Load riddle (always shown)
        var riddle = await _db.Riddles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == currentStage.RiddleId, ct);

        RiddleProjection? riddleProjection = riddle is not null
            ? new RiddleProjection(riddle.Id, riddle.QuestionText, riddle.Location)
            : null;

        // Question only shown when ACTIVE
        QuestionProjection? questionProjection = null;
        if (stageStatus == Domain.Enums.StageStatus.Active)
        {
            var question = await _db.Questions.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == currentStage.QuestionId, ct);

            if (question is not null)
            {
                questionProjection = new QuestionProjection(
                    question.Id, question.Title, question.Description,
                    question.InputFormat, question.OutputFormat, question.Points,
                    question.TimeLimitSeconds);
            }
        }

        return new GameStateProjection(
            team.Id, team.Name, team.Score.Value, currentStageIndex, totalStages,
            statusStr, riddleProjection, questionProjection);
    }

    public async Task<VolunteerGameStateProjection?> GetVolunteerGameStateAsync(Guid teamId, Guid eventId, CancellationToken ct = default)
    {
        var gameState = await GetTeamGameStateAsync(teamId, eventId, ct);
        if (gameState is null) return null;

        // For volunteer: also fetch riddle answer (decrypted in application layer)
        var currentStage = await _db.EventStages.AsNoTracking()
            .FirstOrDefaultAsync(es => es.EventId == eventId && es.StageIndex == gameState.CurrentStageIndex, ct);

        RiddleWithAnswerProjection? riddleWithAnswer = null;
        if (currentStage is not null)
        {
            var riddle = await _db.Riddles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == currentStage.RiddleId, ct);

            if (riddle is not null)
            {
                riddleWithAnswer = new RiddleWithAnswerProjection(
                    riddle.Id, riddle.QuestionText,
                    riddle.AnswerEncrypted, // Will be decrypted in handler
                    riddle.Location);
            }
        }

        return new VolunteerGameStateProjection(
            gameState.TeamId, gameState.TeamName,
            gameState.CurrentStageIndex, gameState.StageStatus,
            riddleWithAnswer);
    }
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;
    public AuditLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
        => await _db.AuditLogs.AddAsync(log, ct);

    public async Task<IList<AuditLog>> GetRecentAsync(Guid eventId, int limit = 50, CancellationToken ct = default)
        => await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IList<AuditLog>> GetForActorAsync(Guid actorId, int limit = 20, CancellationToken ct = default)
        => await _db.AuditLogs.AsNoTracking()
            .Where(a => a.ActorId == actorId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
}

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _db;
    public OutboxRepository(AppDbContext db) => _db = db;

    public async Task<IList<OutboxMessage>> GetPendingAsync(int batchSize = 20, CancellationToken ct = default)
        => await _db.OutboxMessages
            .Where(o => o.ProcessedAt == null && o.RetryCount < 3)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
        => await _db.OutboxMessages.AddAsync(message, ct);

    public void Update(OutboxMessage message) => _db.OutboxMessages.Update(message);
}

public sealed class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly AppDbContext _db;
    public FeatureFlagRepository(AppDbContext db) => _db = db;

    public async Task<bool> IsEnabledAsync(string key, Guid? eventId = null, CancellationToken ct = default)
    {
        var flag = await _db.FeatureFlags.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == key && f.EventId == eventId, ct);
        return flag?.IsEnabled ?? false;
    }

    public async Task<FeatureFlag?> GetByKeyAsync(string key, Guid? eventId = null, CancellationToken ct = default)
        => await _db.FeatureFlags.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == key && f.EventId == eventId, ct);

    public async Task<IList<FeatureFlag>> GetAllAsync(Guid? eventId = null, CancellationToken ct = default)
        => await _db.FeatureFlags.AsNoTracking()
            .Where(f => f.EventId == eventId)
            .ToListAsync(ct);

    public async Task AddAsync(FeatureFlag flag, CancellationToken ct = default)
        => await _db.FeatureFlags.AddAsync(flag, ct);

    public void Update(FeatureFlag flag) => _db.FeatureFlags.Update(flag);
}

public sealed class AnnouncementRepository : IAnnouncementRepository
{
    private readonly AppDbContext _db;
    public AnnouncementRepository(AppDbContext db) => _db = db;

    public async Task<IList<Announcement>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
        => await _db.Announcements.AsNoTracking()
            .Where(a => a.EventId == eventId && a.IsPublished)
            .OrderByDescending(a => a.Priority).ThenByDescending(a => a.PublishedAt)
            .ToListAsync(ct);

    public async Task<IList<Announcement>> GetGlobalAsync(CancellationToken ct = default)
        => await _db.Announcements.AsNoTracking()
            .Where(a => a.IsGlobal && a.IsPublished)
            .ToListAsync(ct);

    public async Task<IList<Announcement>> GetScheduledReadyAsync(CancellationToken ct = default)
        => await _db.Announcements
            .Where(a => !a.IsPublished && a.ScheduledAt <= DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task AddAsync(Announcement announcement, CancellationToken ct = default)
        => await _db.Announcements.AddAsync(announcement, ct);

    public void Update(Announcement announcement) => _db.Announcements.Update(announcement);
}

public sealed class VolunteerAssignmentRepository : IVolunteerAssignmentRepository
{
    private readonly AppDbContext _db;
    public VolunteerAssignmentRepository(AppDbContext db) => _db = db;

    public async Task<VolunteerAssignment?> GetAssignmentAsync(Guid volunteerId, Guid eventId, CancellationToken ct = default)
        => await _db.VolunteerAssignments.AsNoTracking()
            .FirstOrDefaultAsync(va => va.VolunteerId == volunteerId && va.EventId == eventId && va.IsActive, ct);

    public async Task AddAsync(VolunteerAssignment assignment, CancellationToken ct = default)
        => await _db.VolunteerAssignments.AddAsync(assignment, ct);

    public void Update(VolunteerAssignment assignment) => _db.VolunteerAssignments.Update(assignment);
}
