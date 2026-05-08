using TechTrek.Domain.Entities;
using TechTrek.Domain.Enums;
using TechTrek.Domain.Events;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Primitives;
using TechTrek.Domain.ValueObjects;

namespace TechTrek.Domain.Aggregates;

/// <summary>
/// USER AGGREGATE ROOT
/// 
/// Invariants enforced at domain level:
/// - Username must be unique (enforced via DB index + domain check in UserRepository)
/// - Email must be encrypted before storage
/// - Password must be hashed (BCrypt) before storage
/// - Cannot change role without superadmin privilege (business rule enforced in command handler)
/// - Email verification must happen before login is permitted for PARTICIPANT role
/// 
/// Relationships managed:
/// - User can belong to one Team (as PARTICIPANT) via TeamMember
/// - User can be assigned to one Team per Event (as VOLUNTEER) via VolunteerAssignment
/// </summary>
public sealed class User : AggregateRoot
{
    // --- Properties ---
    public string Username { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public EncryptedField Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? College { get; private set; }
    public string? PhoneEncrypted { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF navigation
    public List<TeamMember> TeamMemberships { get; private set; } = new();
    public List<RefreshToken> RefreshTokens { get; private set; } = new();

    private User() { }

    /// <summary>Factory method - the ONLY way to create a new User.</summary>
    public static User Create(
        string username,
        string displayName,
        EncryptedField email,
        string passwordHash,
        UserRole role,
        Guid createdBy = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name cannot be empty.");

        var user = new User
        {
            Username = username.Trim().ToLower(),
            DisplayName = displayName.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsEmailVerified = false,
            CreatedBy = createdBy == default ? null : createdBy
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Username, role));

        return user;
    }

    public void UpdateProfile(string displayName, string? college, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name cannot be empty.");

        DisplayName = displayName.Trim();
        College = college?.Trim();
        AvatarUrl = avatarUrl?.Trim();
        Touch();
    }

    public void VerifyEmail()
    {
        if (IsEmailVerified) return;
        IsEmailVerified = true;
        Touch();
        RaiseDomainEvent(new EmailVerifiedEvent(Id));
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");
        PasswordHash = newPasswordHash;
        Touch();
    }

    public void Deactivate(Guid deactivatedBy)
    {
        IsActive = false;
        SoftDelete(deactivatedBy);
    }

    public void ChangeRole(UserRole newRole, Guid changedBy)
    {
        Role = newRole;
        UpdatedBy = changedBy;
        Touch();
    }
}
