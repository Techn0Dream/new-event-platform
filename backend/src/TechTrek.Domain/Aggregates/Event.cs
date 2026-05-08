using TechTrek.Domain.Enums;
using TechTrek.Domain.Events;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Primitives;

namespace TechTrek.Domain.Aggregates;

/// <summary>
/// EVENT AGGREGATE ROOT
/// 
/// The central organizing entity for TechTrek.
/// Everything is scoped to an Event: teams, questions, riddles, leaderboard, volunteers.
/// 
/// Supports multiple concurrent events (not just single TechTrek event).
/// 
/// Admin operations:
/// - Create/publish event
/// - Start/stop global timer
/// - Pause/resume event
/// - Reset event (soft reset - doesn't delete data, just resets progress)
/// 
/// Game structure:
/// - Event has N stages (ordered by StageIndex)
/// - Each stage has one Question + one Riddle
/// - Teams progress through stages linearly
/// </summary>
public sealed class Event : AggregateRoot
{
    // --- Core Properties ---
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public EventType Type { get; private set; }
    public EventStatus Status { get; private set; } = EventStatus.Draft;
    public int MaxTeamSize { get; private set; } = 4;
    public int TotalStages { get; private set; }

    // --- Timer Properties ---
    public DateTime? TimerStartedAt { get; private set; }
    public int TimerDurationSeconds { get; private set; } = 3600; // Default 1 hour

    // --- Computed ---
    public bool IsTimerRunning => TimerStartedAt.HasValue && Status == EventStatus.Active;
    public int? RemainingSeconds
    {
        get
        {
            if (!IsTimerRunning) return null;
            var elapsed = (int)(DateTime.UtcNow - TimerStartedAt!.Value).TotalSeconds;
            return Math.Max(0, TimerDurationSeconds - elapsed);
        }
    }

    // --- Children ---
    private readonly List<EventStage> _stages = new();
    public IReadOnlyList<EventStage> Stages => _stages.AsReadOnly();

    private Event() { }

    public static Event Create(
        string name,
        string? description,
        EventType type,
        int timerDurationSeconds,
        int maxTeamSize,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Event name cannot be empty.");
        if (timerDurationSeconds <= 0)
            throw new DomainException("Timer duration must be positive.");

        var evt = new Event
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Type = type,
            TimerDurationSeconds = timerDurationSeconds,
            MaxTeamSize = maxTeamSize,
            Status = EventStatus.Draft,
            CreatedBy = createdBy
        };

        evt.RaiseDomainEvent(new EventCreatedEvent(evt.Id, name, type));
        return evt;
    }

    public void Publish()
    {
        if (Status != EventStatus.Draft)
            throw new DomainException("Only draft events can be published.");
        if (!_stages.Any())
            throw new DomainException("Cannot publish an event with no stages.");

        Status = EventStatus.RegistrationOpen;
        Touch();
        RaiseDomainEvent(new EventPublishedEvent(Id, Name));
    }

    public void Start(Guid startedBy)
    {
        if (Status != EventStatus.RegistrationOpen)
            throw new DomainException("Event must be in RegistrationOpen status to start.");

        Status = EventStatus.Active;
        TimerStartedAt = DateTime.UtcNow;
        Touch(startedBy);
        RaiseDomainEvent(new EventStartedEvent(Id, TimerStartedAt.Value, TimerDurationSeconds));
    }

    public void Pause(Guid pausedBy)
    {
        if (Status != EventStatus.Active)
            throw new DomainException("Only active events can be paused.");

        Status = EventStatus.Paused;
        Touch(pausedBy);
    }

    public void Resume(Guid resumedBy)
    {
        if (Status != EventStatus.Paused)
            throw new DomainException("Event is not paused.");

        Status = EventStatus.Active;
        Touch(resumedBy);
    }

    public void Complete(Guid completedBy)
    {
        Status = EventStatus.Completed;
        Touch(completedBy);
        RaiseDomainEvent(new EventCompletedEvent(Id, Name));
    }

    public void AddStage(Guid questionId, Guid riddleId, int stageIndex)
    {
        if (_stages.Any(s => s.StageIndex == stageIndex))
            throw new DomainException($"Stage {stageIndex} already exists.");

        _stages.Add(EventStage.Create(Id, questionId, riddleId, stageIndex));
        TotalStages = _stages.Count;
        Touch();
    }

    public EventStage GetStage(int index)
    {
        return _stages.FirstOrDefault(s => s.StageIndex == index)
            ?? throw new DomainException($"Stage {index} not found in event.");
    }
}

/// <summary>
/// EventStage - maps a (Question, Riddle) pair to a stage index.
/// Child entity of Event aggregate.
/// </summary>
public sealed class EventStage : Entity
{
    public Guid EventId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid RiddleId { get; private set; }
    public int StageIndex { get; private set; }
    public bool IsActive { get; private set; } = true;

    private EventStage() { }

    internal static EventStage Create(Guid eventId, Guid questionId, Guid riddleId, int stageIndex)
    {
        return new EventStage
        {
            EventId = eventId,
            QuestionId = questionId,
            RiddleId = riddleId,
            StageIndex = stageIndex
        };
    }
}
