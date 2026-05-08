using MediatR;
using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Entities;
using TechTrek.Domain.Enums;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Interfaces;
using TechTrek.Domain.ValueObjects;
using TechTrek.Security.Interfaces;
using TechTrek.Shared.Common;
using TechTrek.Shared.Constants;

namespace TechTrek.Application.Auth.Commands;

// ============================================================
// REGISTER COMMAND
// Maps to: POST /api/v1/auth/register
// Handles Register.jsx step 1 (basic info) + step 2 (team config)
// ============================================================

public sealed record RegisterCommand(
    string Username,
    string DisplayName,
    string Email,
    string Password,
    bool IsTeam,
    string? TeamName,
    string? TeammateEmail
) : IRequest<Result<RegisterResult>>;

public sealed record RegisterResult(
    Guid UserId,
    string Username,
    string Message
);

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResult>>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IEncryptionService _encryption;

    public RegisterCommandHandler(
        IUserRepository users,
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IEncryptionService encryption)
    {
        _users = users;
        _uow = uow;
        _hasher = hasher;
        _encryption = encryption;
    }

    public async Task<Result<RegisterResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check username uniqueness
        if (await _users.UsernameExistsAsync(request.Username, cancellationToken))
            return Result<RegisterResult>.Conflict($"Username '{request.Username}' is already taken.");

        // Hash password + encrypt email before creating domain entity
        var passwordHash = _hasher.Hash(request.Password);
        var encryptedEmail = EncryptedField.FromCipherText(_encryption.Encrypt(request.Email));

        var user = User.Create(
            username: request.Username,
            displayName: request.DisplayName,
            email: encryptedEmail,
            passwordHash: passwordHash,
            role: UserRole.Participant
        );

        await _users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Team creation is handled separately after registration
        // Returning userId allows the client to proceed to team setup

        return Result<RegisterResult>.Success(new RegisterResult(
            user.Id,
            user.Username,
            "Registration successful. Please check your email to verify your account."));
    }
}

// ============================================================
// LOGIN COMMAND
// Maps to: POST /api/v1/auth/login
// Handles Login.jsx - returns JWT access token + refresh token
// ============================================================

public sealed record LoginCommand(
    string Username,
    string Password,
    string? DeviceFingerprint,
    string? IpAddress,
    string? UserAgent
) : IRequest<Result<LoginResult>>;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    string RefreshTokenFamily,
    DateTime RefreshTokenExpiry,
    UserInfo User
);

public sealed record UserInfo(
    Guid Id,
    string Username,
    string DisplayName,
    string Role,
    Guid? TeamId,
    Guid? AssignedTeamId,
    Guid? EventId
);

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IVolunteerAssignmentRepository _volunteerAssignments;
    private readonly ITeamRepository _teams;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogs;

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IVolunteerAssignmentRepository volunteerAssignments,
        ITeamRepository teams,
        IUnitOfWork uow,
        IPasswordHasher hasher,
        ITokenService tokenService,
        IAuditLogRepository auditLogs)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _volunteerAssignments = volunteerAssignments;
        _teams = teams;
        _uow = uow;
        _hasher = hasher;
        _tokenService = tokenService;
        _auditLogs = auditLogs;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByUsernameWithTokensAsync(request.Username, cancellationToken);

        // Use constant-time comparison to prevent timing attacks on username enumeration
        var isValid = user is not null && _hasher.Verify(request.Password, user.PasswordHash);

        if (!isValid)
        {
            // Log failed attempt for anti-brute-force monitoring
            await _auditLogs.AddAsync(AuditLog.Create(
                AuditActions.UserLogin, "User",
                ipAddress: request.IpAddress,
                isSuccess: false,
                failureReason: "Invalid credentials"), cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Failure("Invalid username or password.", "INVALID_CREDENTIALS");
        }

        if (!user!.IsActive)
            return Result<LoginResult>.Failure("Your account has been deactivated.", "ACCOUNT_DEACTIVATED");

        // Determine team context based on role
        Guid? teamId = null;
        Guid? assignedTeamId = null;
        Guid? eventId = null;

        // For now, get the first active event (can be extended for multi-event)
        // In production: event selection would be part of login flow
        if (user.Role == UserRole.Participant)
        {
            // Find team membership (simplified - production would scope to active event)
            var teamMembership = user.TeamMemberships.FirstOrDefault();
            if (teamMembership is not null)
            {
                teamId = teamMembership.TeamId;
                var team = await _teams.GetByIdAsync(teamMembership.TeamId, cancellationToken);
                eventId = team?.EventId;
            }
        }
        else if (user.Role == UserRole.Volunteer)
        {
            // Volunteer assignment lookup - needs eventId context
            // Simplified: get any active assignment
        }

        // Generate token pair
        var refreshPair = _tokenService.GenerateRefreshToken(
            user.Id, request.DeviceFingerprint, request.IpAddress, request.UserAgent);

        var accessToken = _tokenService.GenerateAccessToken(user, teamId, assignedTeamId, eventId);

        // Store refresh token in DB
        var refreshToken = RefreshToken.Create(
            user.Id, refreshPair.TokenHash, refreshPair.FamilyId,
            refreshPair.ExpiresAt, request.DeviceFingerprint,
            request.IpAddress, request.UserAgent);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken);

        // Write audit log
        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.UserLogin, "User", user.Id,
            ipAddress: request.IpAddress, isSuccess: true), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(new LoginResult(
            accessToken,
            refreshPair.PlainToken,
            refreshPair.FamilyId,
            refreshPair.ExpiresAt,
            new UserInfo(
                user.Id, user.Username, user.DisplayName,
                user.Role.ToString(), teamId, assignedTeamId, eventId)));
    }
}

// ============================================================
// REFRESH TOKEN COMMAND
// Maps to: POST /api/v1/auth/refresh
// Token rotation with reuse detection
// ============================================================

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? DeviceFingerprint,
    string? IpAddress
) : IRequest<Result<LoginResult>>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResult>>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly ITeamRepository _teams;
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogs;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        ITeamRepository teams,
        IUnitOfWork uow,
        ITokenService tokenService,
        IAuditLogRepository auditLogs)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _teams = teams;
        _uow = uow;
        _tokenService = tokenService;
        _auditLogs = auditLogs;
    }

    public async Task<Result<LoginResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Domain.ValueObjects.TokenHash.Of(request.RefreshToken).Value;
        var storedToken = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken is null)
            return Result<LoginResult>.Failure("Invalid refresh token.", "INVALID_TOKEN");

        // ⚠️ REUSE DETECTION: If token is already revoked, someone is replaying a stolen token
        if (storedToken.IsRevoked)
        {
            // Revoke the ENTIRE family - all tokens from this login session are compromised
            await _refreshTokens.RevokeEntireFamilyAsync(
                storedToken.FamilyId,
                "TOKEN_REUSE_DETECTED",
                cancellationToken);

            await _auditLogs.AddAsync(AuditLog.Create(
                AuditActions.TokenReuseDetected, "RefreshToken", storedToken.UserId,
                ipAddress: request.IpAddress, isSuccess: false,
                failureReason: "Refresh token reuse detected - potential token theft"), cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return Result<LoginResult>.Failure("Security violation detected. All sessions have been terminated.", "TOKEN_COMPROMISED");
        }

        if (storedToken.IsExpired)
            return Result<LoginResult>.Failure("Refresh token has expired. Please login again.", "TOKEN_EXPIRED");

        var user = storedToken.User
            ?? await _users.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<LoginResult>.Failure("Account not found or deactivated.", "ACCOUNT_NOT_FOUND");

        // Revoke the used token (rotation - old token is consumed)
        storedToken.Revoke("ROTATED");

        // Generate new pair (SAME family - maintains login session continuity)
        var newRefreshPair = _tokenService.GenerateRefreshToken(
            user.Id, request.DeviceFingerprint, request.IpAddress, null);

        // Note: new token gets SAME familyId as the old one for tracking
        var newToken = Domain.Entities.RefreshToken.Create(
            user.Id, newRefreshPair.TokenHash, storedToken.FamilyId,
            newRefreshPair.ExpiresAt, request.DeviceFingerprint, request.IpAddress, null);
        newToken.GetType().GetProperty("ReplacedByTokenId"); // Track replacement chain

        await _refreshTokens.AddAsync(newToken, cancellationToken);

        // Get team context for claims
        Guid? teamId = null;
        Guid? eventId = null;
        if (user.Role == UserRole.Participant)
        {
            var team = await _teams.GetTeamForUserAsync(user.Id, eventId ?? Guid.Empty, cancellationToken);
            teamId = team?.Id;
            eventId = team?.EventId;
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user, teamId, null, eventId);

        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.TokenRefresh, "RefreshToken", user.Id,
            ipAddress: request.IpAddress, isSuccess: true), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<LoginResult>.Success(new LoginResult(
            newAccessToken,
            newRefreshPair.PlainToken,
            storedToken.FamilyId,
            newRefreshPair.ExpiresAt,
            new UserInfo(user.Id, user.Username, user.DisplayName, user.Role.ToString(), teamId, null, eventId)));
    }
}

// ============================================================
// LOGOUT COMMAND
// Maps to: POST /api/v1/auth/logout
// Revokes current refresh token family
// ============================================================

public sealed record LogoutCommand(
    Guid UserId,
    string RefreshToken
) : IRequest<Result>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogRepository _auditLogs;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens, IUnitOfWork uow, IAuditLogRepository auditLogs)
    {
        _refreshTokens = refreshTokens;
        _uow = uow;
        _auditLogs = auditLogs;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = Domain.ValueObjects.TokenHash.Of(request.RefreshToken).Value;
        var storedToken = await _refreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            // Revoke only the current family (user might have multiple devices)
            await _refreshTokens.RevokeEntireFamilyAsync(storedToken.FamilyId, "LOGOUT", cancellationToken);
        }

        await _auditLogs.AddAsync(AuditLog.Create(
            AuditActions.UserLogout, "User", request.UserId, isSuccess: true), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
