namespace TechTrek.Domain.Enums;

/// <summary>
/// Represents the state of a team's progress at a specific stage.
/// Maps directly to the frontend's status field in gameEngine.getTeamState().
/// </summary>
public enum StageStatus
{
    /// <summary>Team has not reached this stage yet.</summary>
    NotStarted = 0,

    /// <summary>Team is at this stage, riddle not yet verified by volunteer.</summary>
    Locked = 1,

    /// <summary>Volunteer verified riddle. Team can now see and solve the question.</summary>
    Active = 2,

    /// <summary>Team submitted correct answer. Stage complete, score awarded.</summary>
    Completed = 3,

    /// <summary>Volunteer used emergency skip. Stage bypassed, no points awarded.</summary>
    Skipped = 4
}

/// <summary>
/// Overall game session / event status.
/// </summary>
public enum EventStatus
{
    /// <summary>Event is being configured, not yet published.</summary>
    Draft = 0,

    /// <summary>Registration is open, event not started.</summary>
    RegistrationOpen = 1,

    /// <summary>Event is live and active.</summary>
    Active = 2,

    /// <summary>Event timer expired or admin ended it.</summary>
    Completed = 3,

    /// <summary>Event paused by admin (feature flag).</summary>
    Paused = 4,

    /// <summary>Event archived for historical viewing.</summary>
    Archived = 5
}

/// <summary>
/// Type of event/competition on the platform.
/// </summary>
public enum EventType
{
    TreasureHunt = 0,
    HackathonWithTreasureHunt = 1,
    CodingContest = 2,
    QuizContest = 3,
    TechnicalEvent = 4
}

/// <summary>
/// Team member roles within a team.
/// </summary>
public enum TeamMemberRole
{
    Leader = 0,
    Member = 1
}
