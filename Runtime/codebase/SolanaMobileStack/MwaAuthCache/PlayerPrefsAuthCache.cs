using System;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    /// <summary>
    /// Default <see cref="IMwaAuthCache"/> implementation using Unity's PlayerPrefs.
    /// Tokens are stored as plain-text strings.
    /// 
    /// <para>
    /// For production games that require stronger security, implement <see cref="IMwaAuthCache"/>
    /// with a platform-specific encrypted keystore backend.
    /// </para>
    /// </summary>
    public class PlayerPrefsAuthCache : IMwaAuthCache
    {
        private const string KeyPrefix = "mwa_auth_token_";

        /// <inheritdoc/>
        public Task<string> GetAuthToken(string walletIdentity)
        {
            ValidateIdentity(walletIdentity);
            var key = BuildKey(walletIdentity);
            var token = PlayerPrefs.GetString(key, null);
            return Task.FromResult(string.IsNullOrEmpty(token) ? null : token);
        }

        /// <inheritdoc/>
        public Task SetAuthToken(string walletIdentity, string token)
        {
            ValidateIdentity(walletIdentity);
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Token must not be null or empty.", nameof(token));

            var key = BuildKey(walletIdentity);
            PlayerPrefs.SetString(key, token);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ClearAuthToken(string walletIdentity)
        {
            ValidateIdentity(walletIdentity);
            var key = BuildKey(walletIdentity);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        private static string BuildKey(string walletIdentity) => KeyPrefix + walletIdentity;

        private static void ValidateIdentity(string walletIdentity)
        {
            if (string.IsNullOrEmpty(walletIdentity))
                throw new ArgumentException(
                    "walletIdentity must not be null or empty — this would produce a shared cache key collision.",
                    nameof(walletIdentity));
        }
    }
}
