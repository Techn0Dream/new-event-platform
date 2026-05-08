using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TechTrek.Security.Interfaces;

namespace TechTrek.Security.Services;

/// <summary>
/// BCrypt Password Hasher
/// Uses BCrypt with work factor 12 (adjustable for security/performance trade-off).
/// BCrypt is resistant to brute-force attacks and GPU acceleration.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.");

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

/// <summary>
/// AES-256-CBC Encryption Service for PII fields (email, phone, riddle answers).
/// 
/// Security details:
/// - AES-256 (256-bit key)
/// - CBC mode with random IV per encryption operation
/// - IV is prepended to ciphertext (safe to store together)
/// - Key loaded from environment variable / secret vault (never from appsettings.json in production)
/// - Key rotation: when key changes, a migration job re-encrypts all encrypted fields
/// 
/// WARNING: The EncryptionKey must be exactly 32 bytes (256 bits) when decoded.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IOptions<EncryptionSettings> settings)
    {
        var base64Key = settings.Value.Key
            ?? throw new InvalidOperationException("Encryption key is not configured. Set ENCRYPTION__KEY environment variable.");

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
            throw new InvalidOperationException($"Encryption key must be 32 bytes (256-bit). Got {_key.Length} bytes.");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = _key;
        aes.GenerateIV(); // Fresh random IV for every encryption

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext: [IV (16 bytes)][CipherText]
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var allBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = _key;

        // Extract IV from the beginning
        var iv = new byte[16];
        var cipher = new byte[allBytes.Length - 16];
        Buffer.BlockCopy(allBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(allBytes, 16, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}

public sealed class EncryptionSettings
{
    public string Key { get; init; } = default!;
}
