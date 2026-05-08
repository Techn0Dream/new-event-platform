using TechTrek.Domain.Enums;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Primitives;

namespace TechTrek.Domain.Aggregates;

/// <summary>
/// TeamMember - owned child of Team aggregate.
/// Not an aggregate root itself. Always accessed through Team.
/// </summary>
public sealed class TeamMember : Entity
{
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public TeamMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // EF navigation
    public Team? Team { get; private set; }
    public User? User { get; private set; }

    private TeamMember() { }

    internal static TeamMember Create(Guid teamId, Guid userId, TeamMemberRole role)
    {
        return new TeamMember
        {
            TeamId = teamId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// StageProgress - tracks a team's progress through one stage.
/// Owned child of Team aggregate.
/// Maps to frontend: { status: 'LOCKED' | 'ACTIVE' | 'COMPLETED' | 'SKIPPED' }
/// </summary>
public sealed class StageProgress : Entity
{
    public Guid TeamId { get; private set; }
    public int StageIndex { get; private set; }
    public StageStatus Status { get; private set; }
    public int PointsAwarded { get; private set; }
    public string? SubmittedCode { get; private set; }
    public DateTime? UnlockedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? UnlockedBy { get; private set; }  // Volunteer who verified
    public Guid? CompletedBy { get; private set; } // Participant who submitted

    // EF navigation
    public Team? Team { get; private set; }

    private StageProgress() { }

    internal static StageProgress CreateLocked(Guid teamId, int stageIndex)
    {
        return new StageProgress
        {
            TeamId = teamId,
            StageIndex = stageIndex,
            Status = StageStatus.Locked
        };
    }

    internal void Unlock(Guid volunteerId)
    {
        Status = StageStatus.Active;
        UnlockedAt = DateTime.UtcNow;
        UnlockedBy = volunteerId;
    }

    internal void Complete(int points, string submittedCode, Guid userId)
    {
        Status = StageStatus.Completed;
        PointsAwarded = points;
        SubmittedCode = submittedCode;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = userId;
    }

    internal void Skip(Guid volunteerId)
    {
        Status = StageStatus.Skipped;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = volunteerId;
    }
}
