using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Primitives;

namespace TechTrek.Domain.Entities;

/// <summary>
/// VolunteerAssignment - links a VOLUNTEER user to a specific team for a specific event.
/// Maps to frontend: volunteer.assignedTeamId
/// Scoped per event (same volunteer can be assigned to different teams in different events).
/// </summary>
public sealed class VolunteerAssignment : AuditableEntity
{
    public Guid VolunteerId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public User? Volunteer { get; private set; }
    public Aggregates.Team? Team { get; private set; }
    public Aggregates.Event? Event { get; private set; }

    private VolunteerAssignment() { }

    public static VolunteerAssignment Create(Guid volunteerId, Guid teamId, Guid eventId, Guid assignedBy)
    {
        return new VolunteerAssignment
        {
            VolunteerId = volunteerId,
            TeamId = teamId,
            EventId = eventId,
            CreatedBy = assignedBy
        };
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Announcement entity.
/// Supports global (all participants) and event-scoped announcements.
/// Can be scheduled for future delivery.
/// Maps to frontend: System Logs feed in AdminDashboard.
/// </summary>
public sealed class Announcement : AuditableEntity
{
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public int Priority { get; private set; } = 0; // 0=Normal, 1=High, 2=Critical
    public Guid? EventId { get; private set; }      // null = global
    public bool IsGlobal { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    // Navigation
    public Aggregates.Event? Event { get; private set; }

    private Announcement() { }

    public static Announcement Create(
        string title,
        string body,
        Guid? eventId,
        bool isGlobal,
        int priority = 0,
        DateTime? scheduledAt = null)
    {
        return new Announcement
        {
            Title = title.Trim(),
            Body = body.Trim(),
            EventId = eventId,
            IsGlobal = isGlobal,
            Priority = priority,
            ScheduledAt = scheduledAt,
            IsPublished = scheduledAt is null
        };
    }

    public void Publish()
    {
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        Touch();
    }
}

/// <summary>
/// AuditLog entity.
/// Records every sensitive action: login, logout, admin actions, role changes, resets.
/// Append-only (never updated or deleted).
/// Used by AdminDashboard System Logs feed.
/// </summary>
public sealed class AuditLog : Entity
{
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public Guid? EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? PayloadJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? FailureReason { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        string entityType,
        Guid? actorId = null,
        Guid? entityId = null,
        string? ipAddress = null,
        string? payloadJson = null,
        bool isSuccess = true,
        string? failureReason = null)
    {
        return new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            PayloadJson = payloadJson,
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// OutboxMessage - implements the Transactional Outbox Pattern.
/// 
/// WHY: When we commit a DB transaction (e.g., team completes a stage),
/// we also need to publish a domain event (e.g., to SignalR, to n8n).
/// If we publish directly, and the broker fails AFTER the DB commit,
/// the event is lost. If we publish BEFORE, and the DB fails, we have a ghost event.
/// 
/// Solution: Write the event to the Outbox table IN THE SAME TRANSACTION as the DB changes.
/// A background worker (OutboxDispatcher) reads pending outbox messages and publishes them.
/// This guarantees at-least-once delivery.
/// </summary>
public sealed class OutboxMessage : Entity
{
    public string EventType { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    public bool IsProcessed => ProcessedAt.HasValue;

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payloadJson)
    {
        return new OutboxMessage
        {
            EventType = eventType,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}

/// <summary>
/// FeatureFlag entity - operational control during live events.
/// Examples: freeze_submissions, disable_leaderboard, maintenance_mode
/// </summary>
public sealed class FeatureFlag : Entity
{
    public string Key { get; private set; } = default!;
    public bool IsEnabled { get; private set; }
    public string? Description { get; private set; }
    public Guid? EventId { get; private set; } // null = global
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private FeatureFlag() { }

    public static FeatureFlag Create(string key, bool isEnabled, string? description = null, Guid? eventId = null)
    {
        return new FeatureFlag
        {
            Key = key.ToLower().Trim(),
            IsEnabled = isEnabled,
            Description = description,
            EventId = eventId,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Toggle(Guid updatedBy)
    {
        IsEnabled = !IsEnabled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SetValue(bool value, Guid updatedBy)
    {
        IsEnabled = value;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
