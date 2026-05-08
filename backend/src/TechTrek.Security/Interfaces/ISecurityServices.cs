using TechTrek.Domain.Aggregates;

namespace TechTrek.Security.Interfaces;

/// <summary>Token service interface - generates and validates JWT access tokens + refresh tokens.</summary>
public interface ITokenService
{
    string GenerateAccessToken(User user, Guid? teamId = null, Guid? assignedTeamId = null, Guid? eventId = null);
    RefreshTokenPair GenerateRefreshToken(Guid userId, string? deviceFingerprint, string? ipAddress, string? userAgent);
    ClaimsPrincipalResult? ValidateAccessToken(string token);
}

/// <summary>Password hashing interface - abstracts BCrypt implementation.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Encryption service interface - AES-256 for PII fields.</summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

// ============================================================
// Transfer Objects for Security Layer
// ============================================================

public sealed record RefreshTokenPair(
    string PlainToken,       // Sent to client (in HttpOnly cookie)
    string TokenHash,        // Stored in DB (SHA-256 hash of PlainToken)
    string FamilyId,         // Token rotation family
    DateTime ExpiresAt
);

public sealed record ClaimsPrincipalResult(
    Guid UserId,
    string Username,
    string Role,
    Guid? TeamId,
    Guid? AssignedTeamId,
    string? TokenId,
    string? TokenFamily
);
