using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    /// <summary>
    /// A no-op stub implementation of <see cref="IMwaAuthCache"/> intended as a 
    /// template for encrypted or platform-specific storage backends.
    /// 
    /// Replace the method bodies with your secure storage provider.
    /// Examples:
    /// - Android Keystore (via plugin)  
    /// - iOS Keychain (via plugin)
    /// - A remote wallet-server token store
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
            return Task.FromResult<string>(null);
        }

        /// <inheritdoc/>
        public Task SetAuthToken(string walletIdentity, string token)
        {
            // TODO: persist to your encrypted storage
            // Example: return _secureStorage.SetAsync(walletIdentity, token);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ClearAuthToken(string walletIdentity)
        {
            // TODO: remove from your encrypted storage
            // Example: return _secureStorage.RemoveAsync(walletIdentity);
            return Task.CompletedTask;
        }
    }
}
