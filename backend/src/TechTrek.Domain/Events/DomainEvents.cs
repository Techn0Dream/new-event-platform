using TechTrek.Domain.Enums;
using TechTrek.Domain.Primitives;

namespace TechTrek.Domain.Events;

// ============================================================
// AUTH DOMAIN EVENTS
// ============================================================

public sealed record UserRegisteredEvent(
    Guid UserId,
    string Username,
    UserRole Role
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record EmailVerifiedEvent(
    Guid UserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// ============================================================
// TEAM DOMAIN EVENTS
// ============================================================

public sealed record TeamCreatedEvent(
    Guid TeamId,
    string TeamName,
    Guid EventId
) : IDomainEvent
{
    public Guid EventId_ { get; } = EventId; // Shadows INotification.EventId
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when volunteer unlocks a stage (LOCKED → ACTIVE).
/// SignalR GameHub broadcasts to:
///   - Team group: participant sees question become available
///   - Volunteer group: sees team is now solving
/// </summary>
public sealed record StageUnlockedEvent(
    Guid TeamId,
    Guid EventId,
    int StageIndex,
    Guid VolunteerId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when participant submits correct answer (ACTIVE → COMPLETED).
/// SignalR LeaderboardHub broadcasts to:
///   - Admin dashboard: live rankings update
///   - Participant: sees score increase + next stage
/// Redis leaderboard cache is invalidated.
/// </summary>
public sealed record QuestionCompletedEvent(
    Guid TeamId,
    Guid EventId,
    int StageIndex,
    int PointsAwarded,
    int NewTotalScore
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record StageSkippedEvent(
    Guid TeamId,
    Guid EventId,
    int StageIndex,
    Guid VolunteerId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record TeamFinishedEvent(
    Guid TeamId,
    Guid EventId,
    int FinalScore
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// ============================================================
// EVENT DOMAIN EVENTS
// ============================================================

public sealed record EventCreatedEvent(
    Guid EventId,
    string EventName,
    EventType EventType
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record EventPublishedEvent(
    Guid EventId,
    string EventName
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Fired when admin starts the event.
/// SignalR broadcasts to ALL connected clients: event is live, timer starts.
/// </summary>
public sealed record EventStartedEvent(
    Guid EventId,
    DateTime StartedAt,
    int DurationSeconds
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record EventCompletedEvent(
    Guid EventId,
    string EventName
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
