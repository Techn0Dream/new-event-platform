namespace TechTrek.Shared.Constants;

public static class AuthorizationPolicies
{
    public const string ParticipantOnly = "ParticipantOnly";
    public const string VolunteerOnly = "VolunteerOnly";
    public const string AdminOnly = "AdminOnly";
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string StaffOrAbove = "StaffOrAbove";       // Volunteer, Organizer, Admin, SuperAdmin
    public const string OwnTeamOnly = "OwnTeamOnly";         // teamId claim must match resource teamId
    public const string AssignedTeamOnly = "AssignedTeamOnly"; // volunteer's assignedTeamId must match
}

public static class ClaimTypes
{
    public const string UserId = "uid";
    public const string Role = "role";
    public const string TeamId = "tid";
    public const string AssignedTeamId = "atid";
    public const string EventId = "eid";
    public const string TokenFamily = "tfam";
    public const string TokenId = "jti";
}

public static class CacheKeys
{
    public static string Leaderboard(Guid eventId) => $"leaderboard:{eventId}";
    public static string TeamGameState(Guid teamId) => $"game:team:{teamId}:state";
    public static string EventState(Guid eventId) => $"event:{eventId}:state";
    public static string FeatureFlag(string key, Guid? eventId) =>
        eventId.HasValue ? $"ff:{key}:event:{eventId}" : $"ff:{key}:global";
    public static string TokenRevoked(string jti) => $"token:revoked:{jti}";
}

public static class SignalRGroups
{
    public static string TeamGame(Guid teamId) => $"team:{teamId}:game";
    public static string EventLeaderboard(Guid eventId) => $"event:{eventId}:leaderboard";
    public static string AdminEvent(Guid eventId) => $"admin:{eventId}";
}

public static class FeatureFlags
{
    public const string FreezeSubmissions = "freeze_submissions";
    public const string DisableLeaderboard = "disable_leaderboard";
    public const string MaintenanceMode = "maintenance_mode";
    public const string EnableRealtime = "enable_realtime";
    public const string DisableRegistrations = "disable_registrations";
    public const string PauseEvent = "pause_event";
}

public static class AuditActions
{
    public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string UserRegister = "USER_REGISTER";
    public const string TokenRefresh = "TOKEN_REFRESH";
    public const string TokenReuseDetected = "TOKEN_REUSE_DETECTED";
    public const string RiddleVerified = "RIDDLE_VERIFIED";
    public const string AnswerSubmitted = "ANSWER_SUBMITTED";
    public const string QuestionSkipped = "QUESTION_SKIPPED";
    public const string EventReset = "EVENT_RESET";
    public const string AdminOverride = "ADMIN_OVERRIDE";
    public const string RoleChanged = "ROLE_CHANGED";
}
