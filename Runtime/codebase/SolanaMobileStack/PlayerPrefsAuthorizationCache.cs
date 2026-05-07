using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace Solana.Unity.SolanaMobileStack
{
    [Preserve]
    public sealed class PlayerPrefsAuthorizationCache : IAuthorizationCache
    {
        internal const string DefaultKey = "SolanaUnity.MWA.AuthorizationRecord.v1";

        private const string LegacyPkKey = "pk";

        private bool _warnedThisSession;

        private readonly string _key;

        public PlayerPrefsAuthorizationCache() : this(null) { }

        public PlayerPrefsAuthorizationCache(string scope)
        {
            _key = string.IsNullOrEmpty(scope)
                ? DefaultKey
                : DefaultKey + "." + scope;
        }

        public Task<AuthorizationRecord> GetAsync()
        {
            string json = PlayerPrefs.GetString(_key, null);
            if (string.IsNullOrEmpty(json))
            {
                return Task.FromResult<AuthorizationRecord>(null);
            }

            try
            {
                var record = JsonConvert.DeserializeObject<AuthorizationRecord>(json);
                return Task.FromResult<AuthorizationRecord>(record);
            }
            catch (Exception)
            {
                if (!_warnedThisSession)
                {
                    _warnedThisSession = true;
                    Debug.LogWarning(
                        $"[PlayerPrefsAuthorizationCache] persisted record at key '{_key}' " +
                        "failed to deserialize; treating as absent. This warning is emitted once per session.");
                }
                return Task.FromResult<AuthorizationRecord>(null);
            }
        }

        public Task SetAsync(AuthorizationRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            string json = JsonConvert.SerializeObject(record);
            PlayerPrefs.SetString(_key, json);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }
    }
}
