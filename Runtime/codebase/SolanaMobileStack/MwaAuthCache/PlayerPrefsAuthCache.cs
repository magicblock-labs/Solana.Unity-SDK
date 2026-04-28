using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK
{
    /// <summary>
    /// Default <see cref="IMwaAuthCache"/>, backed by
    /// <see cref="PlayerPrefs"/>. Uses the same storage key
    /// (<see cref="DefaultKey"/>) the SDK has been writing since PR #269,
    /// so anyone who already has a cached session keeps it after upgrading.
    /// No migration step needed.
    ///
    /// PlayerPrefs is plaintext on Android. Fine for hobby games and demos,
    /// not fine for games that hold real assets - swap this out for a
    /// custom <see cref="IMwaAuthCache"/> backed by Android Keystore or
    /// EncryptedSharedPreferences in that case.
    ///
    /// The optional <paramref name="scope"/> on the second constructor lets
    /// a single app keep independent sessions per wallet identity, e.g.
    /// <c>new PlayerPrefsAuthCache("phantom")</c> writes to
    /// <c>solana_sdk.mwa.auth_token.phantom</c>. Default (no scope) is
    /// backward compatible with existing installs.
    /// </summary>
    [Preserve]
    public class PlayerPrefsAuthCache : IMwaAuthCache
    {
        /// <summary>
        /// Storage key used when no scope is supplied. Public so other
        /// <see cref="IMwaAuthCache"/> implementations can reference the
        /// same key for a one-time copy-up migration if they want to inherit
        /// the existing PlayerPrefs session on first run.
        /// </summary>
        public const string DefaultKey = "solana_sdk.mwa.auth_token";

        private readonly string _key;

        /// <summary>
        /// Creates the default unscoped cache. Equivalent to
        /// <c>new PlayerPrefsAuthCache(null)</c>.
        /// </summary>
        public PlayerPrefsAuthCache() : this(null) { }

        /// <summary>
        /// Creates a cache that namespaces its storage under
        /// <c>{DefaultKey}.{scope}</c>. If <paramref name="scope"/> is null
        /// or empty the default key is used, which keeps backward
        /// compatibility with installs created before scoping existed.
        /// </summary>
        public PlayerPrefsAuthCache(string scope)
        {
            _key = string.IsNullOrEmpty(scope)
                ? DefaultKey
                : DefaultKey + "." + scope;
        }

        /// <inheritdoc />
        public Task<string> Get()
        {
            string value = PlayerPrefs.GetString(_key, null);
            return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
        }

        /// <inheritdoc />
        public Task Set(string authToken)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                return Task.CompletedTask;
            }
            PlayerPrefs.SetString(_key, authToken);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Clear()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }
    }
}
