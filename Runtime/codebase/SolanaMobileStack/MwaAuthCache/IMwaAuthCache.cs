using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    /// <summary>
    /// Extensible interface for persisting Mobile Wallet Adapter authorization tokens.
    /// 
    /// Implement this interface to provide custom token storage backends 
    /// (e.g. encrypted storage, cloud sync, secure keystore).
    /// 
    /// The default implementation is <see cref="PlayerPrefsAuthCache"/> which
    /// uses Unity's PlayerPrefs for simple local persistence.
    /// 
    /// Example custom implementation:
    /// <code>
    /// public class MySecureCache : IMwaAuthCache {
    ///     public Task&lt;string&gt; GetAuthToken(string walletIdentity) { ... }
    ///     public Task SetAuthToken(string walletIdentity, string token) { ... }
    ///     public Task ClearAuthToken(string walletIdentity) { ... }
    /// }
    /// // Then inject it:
    /// var adapter = new SolanaWalletAdapter(options, authCache: new MySecureCache());
    /// </code>
    /// </summary>
    public interface IMwaAuthCache
    {
        /// <summary>
        /// Retrieves the cached auth token for the given wallet identity, or null if none.
        /// </summary>
        /// <param name="walletIdentity">A unique key identifying the wallet (e.g. identity URI + name).</param>
        Task<string> GetAuthToken(string walletIdentity);

        /// <summary>
        /// Stores an auth token for the given wallet identity.
        /// </summary>
        Task SetAuthToken(string walletIdentity, string token);

        /// <summary>
        /// Clears the stored auth token for the given wallet identity.
        /// </summary>
        Task ClearAuthToken(string walletIdentity);
    }
}
