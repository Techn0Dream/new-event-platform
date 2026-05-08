using TechTrek.Domain.Enums;
using TechTrek.Domain.Aggregates;
using TechTrek.Domain.Exceptions;
using TechTrek.Domain.Primitives;
using TechTrek.Domain.ValueObjects;

namespace TechTrek.Domain.Entities;

/// <summary>
/// RefreshToken entity.
/// 
/// Key security design decisions:
/// - Token string is NEVER stored. Only the SHA-256 hash is stored.
/// - Each token belongs to a "family" for rotation tracking.
/// - If a refresh token from a previous rotation is detected (reuse attack),
///   the ENTIRE family is immediately revoked.
/// - Tokens expire after 7 days.
/// - Device fingerprint helps detect cross-device token reuse.
/// 
/// Token family approach prevents token theft via reuse detection.
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;   // SHA-256 hash of actual token
    public string FamilyId { get; private set; } = default!;    // Rotation family ID
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    // Navigation
    public User? User { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        string familyId,
        DateTime expiresAt,
        string? deviceFingerprint,
        string? ipAddress,
        string? userAgent)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAt = expiresAt,
            DeviceFingerprint = deviceFingerprint,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public void Revoke(string reason, Guid? replacedBy = null)
    {
        if (IsRevoked) return;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByTokenId = replacedBy;
    }
}

/// <summary>
/// EmailVerificationToken - for verifying user email addresses.
/// </summary>
public sealed class EmailVerificationToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsExpired && !IsUsed;

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid userId, string tokenHash, int expiryMinutes = 1440)
    {
        return new EmailVerificationToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    public void MarkUsed()
    {
        if (!IsValid) throw new DomainException("Token is expired or already used.");
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// PasswordResetToken entity.
/// </summary>
public sealed class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UsedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsExpired && !IsUsed;

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15) // 15-minute window
        };
    }

    public void MarkUsed()
    {
        if (!IsValid) throw new DomainException("Password reset token is expired or already used.");
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}
