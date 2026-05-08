namespace TechTrek.Domain.Enums;

/// <summary>
/// User roles in TechTrek platform.
/// Maps directly to JWT "role" claim and authorization policies.
/// </summary>
public enum UserRole
{
    /// <summary>Platform god-mode. Can do everything.</summary>
    SuperAdmin = 0,

    /// <summary>Event organizer. Manages events, questions, riddles, announcements.</summary>
    Admin = 1,

    /// <summary>Event staff assigned to specific teams. Verifies riddles, can skip questions.</summary>
    Volunteer = 2,

    /// <summary>Competition participant. Member of a team, submits code, solves riddles.</summary>
    Participant = 3,

    /// <summary>Organizer role: creates events but lower privilege than Admin.</summary>
    Organizer = 4
}
