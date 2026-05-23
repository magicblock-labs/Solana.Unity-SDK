using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK
{
    /// <summary>
    /// Where the Mobile Wallet Adapter auth token gets stored.
    ///
    /// By default the SDK uses <see cref="PlayerPrefsAuthCache"/>, which
    /// writes the token into <see cref="UnityEngine.PlayerPrefs"/>. That is
    /// fine for most games but PlayerPrefs is plaintext on Android, so games
    /// that hold real on-chain value should plug in a custom implementation
    /// backed by Android Keystore / EncryptedSharedPreferences (or iOS
    /// Keychain in the future).
    ///
    /// We only persist the auth token here. The public key is not a secret,
    /// so it stays in PlayerPrefs and is out of scope for this interface.
    ///
    /// Implementations should keep all three methods cheap. <see cref="Clear"/>
    /// is awaited synchronously from <c>Logout()</c>, so do not block on UI
    /// or network calls inside it.
    /// </summary>
    public interface IMwaAuthCache
    {
        /// <summary>
        /// Returns the stored auth token, or <c>null</c> if nothing is stored
        /// yet. Return <c>null</c> (not an empty string) on a fresh install,
        /// otherwise the SDK cannot tell "never logged in" from "logged in
        /// with empty token".
        /// </summary>
        Task<string> Get();

        /// <summary>
        /// Persists <paramref name="authToken"/>. Treat <c>null</c> or empty
        /// input as a no-op so a stale callsite cannot wipe a valid session
        /// by accident.
        /// </summary>
        Task Set(string authToken);

        /// <summary>
        /// Removes the stored token. Must be idempotent so calling it twice
        /// (e.g. <c>Logout()</c> followed by <c>DisconnectWallet()</c>) does
        /// not throw.
        /// </summary>
        Task Clear();
    }
}
