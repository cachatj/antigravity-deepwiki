namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Config encryption service interface
/// </summary>
public interface IConfigEncryption
{
    /// <summary>
    /// Encrypt config data
    /// </summary>
    /// <param name="plainText">Plaintext data</param>
    /// <returns>Encrypted data</returns>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypt config data
    /// </summary>
    /// <param name="cipherText">Encrypted data</param>
    /// <returns>Decrypted plaintext</returns>
    string Decrypt(string cipherText);
    
    /// <summary>
    /// Check whether data is encrypted
    /// </summary>
    /// <param name="data">Data</param>
    /// <returns>Whether the data is encrypted</returns>
    bool IsEncrypted(string data);
}
