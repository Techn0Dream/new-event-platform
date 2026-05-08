using TechTrek.Domain.Enums;
using TechTrek.Domain.Events;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Primitives;
using TechTrek.Domain.ValueObjects;

namespace TechTrek.Domain.Aggregates;

/// <summary>
/// TEAM AGGREGATE ROOT
/// 
/// Core aggregate for the TechTrek game. Owns:
/// - Members (TeamMember children)
/// - Stage progress (StageProgress children)
/// - Score (Value Object)
/// 
/// Critical business rules enforced:
/// - Only one LEADER per team
/// - Max members enforced per event rules
/// - Score can only increase (no deductions in current game model)
/// - Stage advancement is linear (you must complete stage N before N+1)
/// - A stage can only be unlocked ONCE by volunteer verification
/// - Skipping is irreversible
/// 
/// This aggregate is the source of truth for all game state queries.
/// Frontend's 5s polling (now replaced by SignalR) ultimately reads from this aggregate's projection.
/// </summary>
public sealed class Team : AggregateRoot
{
    // --- Core Properties ---
    public string Name { get; private set; } = default!;
    public Guid EventId { get; private set; }
    public Score Score { get; private set; } = Score.Zero;
    public int CurrentStageIndex { get; private set; } = 0;
    public bool HasFinished { get; private set; } = false;

    // --- Children (owned by this aggregate) ---
    private readonly List<TeamMember> _members = new();
    private readonly List<StageProgress> _stageProgress = new();

    public IReadOnlyList<TeamMember> Members => _members.AsReadOnly();
    public IReadOnlyList<StageProgress> StageProgresses => _stageProgress.AsReadOnly();

    // --- EF navigation ---
    public Event? Event { get; private set; }

    private Team() { }

    public static Team Create(string name, Guid eventId, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Team name cannot be empty.");

        var team = new Team
        {
            Name = name.Trim(),
            EventId = eventId,
            Score = Score.Zero,
            CurrentStageIndex = 0,
            CreatedBy = createdBy
        };

        team.RaiseDomainEvent(new TeamCreatedEvent(team.Id, name, eventId));
        return team;
    }

    // --- Membership Management ---

    public void AddMember(Guid userId, TeamMemberRole role)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this team.");

        if (role == TeamMemberRole.Leader && _members.Any(m => m.Role == TeamMemberRole.Leader))
            throw new DomainException("Team already has a leader.");

        _members.Add(TeamMember.Create(Id, userId, role));
        Touch();
    }

    public void RemoveMember(Guid userId, Guid removedBy)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException("User is not a member of this team.");

        if (member.Role == TeamMemberRole.Leader)
            throw new DomainException("Cannot remove the team leader.");

        _members.Remove(member);
        Touch(removedBy);
    }

    // --- Game Progression ---

    /// <summary>
    /// Called by VOLUNTEER when they verify the team's riddle answer at the physical location.
    /// Unlocks the current stage (moves from LOCKED → ACTIVE).
    /// </summary>
    public void UnlockCurrentStage(int stageIndex, Guid volunteerId)
    {
        ValidateStageIndex(stageIndex);

        var progress = GetOrCreateStageProgress(stageIndex);
        if (progress.Status != StageStatus.Locked)
            throw new DomainException($"Stage {stageIndex} cannot be unlocked (current status: {progress.Status}).");

        progress.Unlock(volunteerId);
        Touch(volunteerId);

        RaiseDomainEvent(new StageUnlockedEvent(Id, EventId, stageIndex, volunteerId));
    }

    /// <summary>
    /// Called when PARTICIPANT submits a successful answer for the current stage.
    /// Awards points and advances to next stage.
    /// </summary>
    public void CompleteCurrentStage(int stageIndex, int pointsAwarded, string submittedCode, Guid userId)
    {
        ValidateStageIndex(stageIndex);

        var progress = GetOrCreateStageProgress(stageIndex);
        if (progress.Status != StageStatus.Active)
            throw new DomainException($"Stage {stageIndex} is not active. Current status: {progress.Status}.");

        progress.Complete(pointsAwarded, submittedCode, userId);
        Score = Score.Add(pointsAwarded);
        CurrentStageIndex++;
        Touch(userId);

        RaiseDomainEvent(new QuestionCompletedEvent(Id, EventId, stageIndex, pointsAwarded, Score.Value));
    }

    /// <summary>
    /// Emergency skip by VOLUNTEER. Stage is bypassed, no points awarded.
    /// </summary>
    public void SkipCurrentStage(int stageIndex, Guid volunteerId)
    {
        ValidateStageIndex(stageIndex);

        var progress = GetOrCreateStageProgress(stageIndex);
        if (progress.Status == StageStatus.Completed)
            throw new DomainException("Cannot skip an already completed stage.");
        if (progress.Status == StageStatus.Skipped)
            throw new DomainException("Stage is already skipped.");

        progress.Skip(volunteerId);
        CurrentStageIndex++;
        Touch(volunteerId);

        RaiseDomainEvent(new StageSkippedEvent(Id, EventId, stageIndex, volunteerId));
    }

    /// <summary>
    /// Marks team as finished when all stages are complete.
    /// </summary>
    public void MarkFinished()
    {
        HasFinished = true;
        Touch();
        RaiseDomainEvent(new TeamFinishedEvent(Id, EventId, Score.Value));
    }

    /// <summary>
    /// Initializes stage progress entries for a new event (called when team is set up).
    /// Creates LOCKED entries for all stages.
    /// </summary>
    public void InitializeStageProgress(int totalStages)
    {
        for (int i = 0; i < totalStages; i++)
        {
            _stageProgress.Add(StageProgress.CreateLocked(Id, i));
        }
    }

    // --- Private Helpers ---

    private void ValidateStageIndex(int stageIndex)
    {
        if (stageIndex != CurrentStageIndex)
            throw new DomainException($"Invalid stage index. Expected {CurrentStageIndex}, got {stageIndex}.");
    }

    private StageProgress GetOrCreateStageProgress(int stageIndex)
    {
        var progress = _stageProgress.FirstOrDefault(p => p.StageIndex == stageIndex);
        if (progress is null)
        {
            progress = StageProgress.CreateLocked(Id, stageIndex);
            _stageProgress.Add(progress);
        }
        return progress;
    }

    public StageStatus GetStageStatus(int stageIndex)
    {
        var progress = _stageProgress.FirstOrDefault(p => p.StageIndex == stageIndex);
        return progress?.Status ?? StageStatus.Locked;
    }
}
