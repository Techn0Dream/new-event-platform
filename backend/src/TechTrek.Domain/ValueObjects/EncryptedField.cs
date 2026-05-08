using System.Security.Cryptography;

namespace TechTrek.Domain.ValueObjects;

/// <summary>
/// Value object for storing encrypted PII fields (email, phone).
/// The raw value is NEVER stored in the database. Only the AES-256 encrypted ciphertext is persisted.
/// Encryption/decryption happens in EncryptionService (Infrastructure), not here.
/// This VO wraps the ciphertext for type safety.
/// </summary>
public sealed record EncryptedField
{
    public string CipherText { get; }

    private EncryptedField(string cipherText)
    {
        CipherText = cipherText;
    }

    public static EncryptedField FromCipherText(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            throw new ArgumentException("Cipher text cannot be empty.", nameof(cipherText));
        return new EncryptedField(cipherText);
    }

    public override string ToString() => "[ENCRYPTED]";
}

/// <summary>
/// Value object for a hashed token (refresh token, verification token).
/// Tokens are always stored as SHA-256 hashes, never in plaintext.
/// </summary>
public sealed record TokenHash
{
    public string Value { get; }

    private TokenHash(string value)
    {
        Value = value;
    }

    public static TokenHash Of(string plainToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainToken);
        var hash = SHA256.HashData(bytes);
        return new TokenHash(Convert.ToBase64String(hash));
    }

    public static TokenHash FromHash(string existingHash) => new(existingHash);

    public bool MatchesPlain(string plainToken)
    {
        var candidate = Of(plainToken);
        return Value == candidate.Value;
    }
}
