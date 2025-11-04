namespace AppliedAccountability.Security.Encryption;

/// <summary>
/// Service for encrypting and decrypting data
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a string value
    /// </summary>
    /// <param name="plainText">Plain text to encrypt</param>
    /// <returns>Encrypted value as base64 string</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string value
    /// </summary>
    /// <param name="cipherText">Encrypted value as base64 string</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Encrypts binary data
    /// </summary>
    /// <param name="plainBytes">Plain bytes to encrypt</param>
    /// <returns>Encrypted bytes</returns>
    byte[] Encrypt(byte[] plainBytes);

    /// <summary>
    /// Decrypts binary data
    /// </summary>
    /// <param name="cipherBytes">Encrypted bytes</param>
    /// <returns>Decrypted bytes</returns>
    byte[] Decrypt(byte[] cipherBytes);

    /// <summary>
    /// Hashes a value using SHA-256
    /// </summary>
    /// <param name="value">Value to hash</param>
    /// <returns>Hash as base64 string</returns>
    string Hash(string value);

    /// <summary>
    /// Verifies a hash matches the original value
    /// </summary>
    /// <param name="value">Original value</param>
    /// <param name="hash">Hash to verify</param>
    /// <returns>True if hash matches</returns>
    bool VerifyHash(string value, string hash);
}

/// <summary>
/// Options for encryption service
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// Encryption key (must be 32 bytes for AES-256)
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use HMAC for integrity verification
    /// </summary>
    public bool UseHmac { get; set; } = true;
}
