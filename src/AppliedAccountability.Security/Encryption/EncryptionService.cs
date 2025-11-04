using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AppliedAccountability.Security.Encryption;

/// <summary>
/// Default implementation of encryption service using AES-256
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _encryptionKey;

    public EncryptionService(
        ILogger<EncryptionService> logger,
        EncryptionOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.EncryptionKey))
        {
            throw new ArgumentException("Encryption key cannot be empty", nameof(options));
        }

        // Ensure key is exactly 32 bytes (256 bits) for AES-256
        _encryptionKey = DeriveKey(options.EncryptionKey, 32);
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = Encrypt(plainBytes);
            var result = Convert.ToBase64String(encryptedBytes);

            _logger.LogDebug("Successfully encrypted string value");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt string value");
            throw;
        }
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = Decrypt(cipherBytes);
            var result = Encoding.UTF8.GetString(decryptedBytes);

            _logger.LogDebug("Successfully decrypted string value");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt string value");
            throw;
        }
    }

    /// <inheritdoc />
    public byte[] Encrypt(byte[] plainBytes)
    {
        ArgumentNullException.ThrowIfNull(plainBytes);

        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();

            // Write IV to the beginning of the stream
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(plainBytes, 0, plainBytes.Length);
            }

            var result = msEncrypt.ToArray();

            _logger.LogDebug("Successfully encrypted {ByteCount} bytes", plainBytes.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt bytes");
            throw;
        }
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] cipherBytes)
    {
        ArgumentNullException.ThrowIfNull(cipherBytes);

        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV from the beginning of the cipher bytes
            var iv = new byte[aes.IV.Length];
            Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msPlain = new MemoryStream();

            csDecrypt.CopyTo(msPlain);
            var result = msPlain.ToArray();

            _logger.LogDebug("Successfully decrypted {ByteCount} bytes", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt bytes");
            throw;
        }
    }

    /// <inheritdoc />
    public string Hash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hashBytes = SHA256.HashData(bytes);
            var result = Convert.ToBase64String(hashBytes);

            _logger.LogDebug("Successfully hashed value");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash value");
            throw;
        }
    }

    /// <inheritdoc />
    public bool VerifyHash(string value, string hash)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(hash);

        try
        {
            var computedHash = Hash(value);
            var result = computedHash.Equals(hash, StringComparison.Ordinal);

            _logger.LogDebug("Hash verification result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify hash");
            return false;
        }
    }

    private static byte[] DeriveKey(string password, int keyLength)
    {
        // Use a fixed salt for deterministic key derivation
        // In production, consider using a per-user salt stored securely
        var salt = Encoding.UTF8.GetBytes("AppliedAccountability.Security");

        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations: 10000,
            HashAlgorithmName.SHA256);

        return deriveBytes.GetBytes(keyLength);
    }
}
