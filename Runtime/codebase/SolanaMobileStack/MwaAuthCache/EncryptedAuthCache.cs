using System;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    /// <summary>
    /// A placeholder implementation of <see cref="IMwaAuthCache"/> that documents
    /// where to integrate an encrypted or platform-specific storage backend.
    /// 
    /// <para>
    /// <b>Do not use this class directly in production.</b> It throws
    /// <see cref="NotImplementedException"/> on every call to make integration gaps visible
    /// immediately during development, rather than silently dropping tokens.
    /// </para>
    /// 
    /// To implement secure storage:
    /// <list type="bullet">
    ///   <item>Android Keystore (via plugin)</item>
    ///   <item>iOS Keychain (via plugin)</item>
    ///   <item>A remote wallet-server token store</item>
    /// </list>
    /// </summary>
    public class EncryptedAuthCache : IMwaAuthCache
    {
        // TODO: inject your encryption provider or secure storage SDK here
        // Example: private readonly ISecureStorage _secureStorage;

        /// <inheritdoc/>
        public Task<string> GetAuthToken(string walletIdentity)
        {
            // TODO: retrieve from your encrypted storage using walletIdentity as key
            // Example: return _secureStorage.GetAsync(walletIdentity);
            throw new NotImplementedException(
                "EncryptedAuthCache is a template. Implement GetAuthToken using your secure storage provider.");
        }

        /// <inheritdoc/>
        public Task SetAuthToken(string walletIdentity, string token)
        {
            // TODO: persist to your encrypted storage
            // Example: return _secureStorage.SetAsync(walletIdentity, token);
            throw new NotImplementedException(
                "EncryptedAuthCache is a template. Implement SetAuthToken using your secure storage provider.");
        }

        /// <inheritdoc/>
        public Task ClearAuthToken(string walletIdentity)
        {
            // TODO: remove from your encrypted storage
            // Example: return _secureStorage.RemoveAsync(walletIdentity);
            throw new NotImplementedException(
                "EncryptedAuthCache is a template. Implement ClearAuthToken using your secure storage provider.");
        }
    }
}
