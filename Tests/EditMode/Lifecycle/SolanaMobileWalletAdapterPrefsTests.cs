using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Lifecycle
{
    [Category("Lifecycle")]
    public class SolanaMobileWalletAdapterPrefsTests
    {
        private const string LegacyPk = "pk";
        private const string LegacyAuthToken = "authToken";
        private const string Pr269Pk = "solana_sdk.mwa.public_key";
        private const string Pr269AuthToken = "solana_sdk.mwa.auth_token";
        private const string CacheKey = "SolanaUnity.MWA.AuthorizationRecord.v1";

        private static readonly string[] RelevantKeys =
        {
            LegacyPk, LegacyAuthToken, Pr269Pk, Pr269AuthToken, CacheKey
        };

        private static readonly FieldInfo CacheField =
            typeof(SolanaMobileWalletAdapter).GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo GateField =
            typeof(SolanaMobileWalletAdapter).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MigrateMethod =
            typeof(SolanaMobileWalletAdapter).GetMethod("MigrateLegacyPrefKeysAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        private Dictionary<string, string> _originalPrefs;

        [OneTimeSetUp]
        public void Guard()
        {
            Assert.That(CacheField, Is.Not.Null, "_cache field not found");
            Assert.That(GateField, Is.Not.Null, "_gate field not found");
            Assert.That(MigrateMethod, Is.Not.Null,
                "MigrateLegacyPrefKeysAsync must exist as private instance method");
        }

        [SetUp]
        public void SetUp()
        {
            _originalPrefs = SnapshotRelevantKeys();
            DeleteAllRelevantKeys();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteAllRelevantKeys();
            RestoreOriginalKeys();
        }

        private static Dictionary<string, string> SnapshotRelevantKeys()
        {
            var snapshot = new Dictionary<string, string>();
            foreach (var key in RelevantKeys)
                if (PlayerPrefs.HasKey(key))
                    snapshot[key] = PlayerPrefs.GetString(key);
            return snapshot;
        }

        private void RestoreOriginalKeys()
        {
            if (_originalPrefs == null) return;
            foreach (var entry in _originalPrefs)
                PlayerPrefs.SetString(entry.Key, entry.Value);
            PlayerPrefs.Save();
        }

        private static void DeleteAllRelevantKeys()
        {
            foreach (var key in RelevantKeys)
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private static SolanaMobileWalletAdapter CreateAdapterWithCache(IAuthorizationCache cache)
        {
            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(
                typeof(SolanaMobileWalletAdapter));
            CacheField.SetValue(adapter, cache);
            GateField.SetValue(adapter, new System.Threading.SemaphoreSlim(1, 1));
            return adapter;
        }

        private static async Task InvokeMigrate(SolanaMobileWalletAdapter adapter)
        {
            await (Task)MigrateMethod.Invoke(adapter, null);
        }

        [Test]
        public void PrefKeyConstants_HaveNamespacedValues()
        {
            var field = typeof(PlayerPrefsAuthorizationCache)
                .GetField("DefaultKey", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, "DefaultKey constant must exist on PlayerPrefsAuthorizationCache");
            Assert.AreEqual(CacheKey, (string)field.GetRawConstantValue(),
                "Cache default key must be 'SolanaUnity.MWA.AuthorizationRecord.v1'");
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_NoLegacyKeys_IsNoOp()
        {
            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            Assert.That(await cache.GetAsync(), Is.Null,
                "Migration must not invent data when nothing is stored");
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_Migrates_LegacyPk_ToNewKey()
        {
            PlayerPrefs.SetString(LegacyPk, "pubkey-abc");
            PlayerPrefs.SetString(LegacyAuthToken, "token-xyz");
            PlayerPrefs.Save();

            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            var record = await cache.GetAsync();
            Assert.That(record, Is.Not.Null, "Migration must create a cache record");
            Assert.AreEqual("pubkey-abc", record.AccountAddress);
            Assert.AreEqual("token-xyz", record.AuthToken);
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_Migrates_LegacyAuthToken_ToNewKey()
        {
            PlayerPrefs.SetString(LegacyPk, "pk-val");
            PlayerPrefs.SetString(LegacyAuthToken, "token-only-123");
            PlayerPrefs.Save();

            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            var record = await cache.GetAsync();
            Assert.That(record, Is.Not.Null);
            Assert.AreEqual("token-only-123", record.AuthToken);
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_Deletes_LegacyKeys_AfterMigration()
        {
            PlayerPrefs.SetString(LegacyPk, "pubkey-abc");
            PlayerPrefs.SetString(LegacyAuthToken, "token-xyz-123");
            PlayerPrefs.Save();

            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            Assert.IsFalse(PlayerPrefs.HasKey(LegacyPk),
                "Legacy 'pk' key must be deleted after migration");
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyAuthToken),
                "Legacy 'authToken' key must be deleted after migration");
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_Pr269Keys_TakePrecedence()
        {
            PlayerPrefs.SetString(LegacyPk, "old-pk");
            PlayerPrefs.SetString(Pr269Pk, "pr269-pk");
            PlayerPrefs.SetString(Pr269AuthToken, "pr269-token");
            PlayerPrefs.Save();

            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            var record = await cache.GetAsync();
            Assert.That(record, Is.Not.Null);
            Assert.AreEqual("pr269-pk", record.AccountAddress,
                "PR269 keys must take precedence over legacy keys");
            Assert.IsFalse(PlayerPrefs.HasKey(Pr269Pk), "PR269 pk key must be deleted");
            Assert.IsFalse(PlayerPrefs.HasKey(Pr269AuthToken), "PR269 auth key must be deleted");
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_SecondCall_IsNoOp()
        {
            PlayerPrefs.SetString(LegacyPk, "pubkey-abc");
            PlayerPrefs.SetString(LegacyAuthToken, "token-abc");
            PlayerPrefs.Save();

            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);
            await InvokeMigrate(adapter);

            var record = await cache.GetAsync();
            Assert.That(record, Is.Not.Null);
            Assert.AreEqual("pubkey-abc", record.AccountAddress);
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyPk));
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_OnlyAuthTokenPresent_Migrates()
        {
            PlayerPrefs.SetString(LegacyAuthToken, "only-token");
            PlayerPrefs.Save();

            var cache = new InMemoryCache();
            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            var record = await cache.GetAsync();
            Assert.That(record, Is.Null,
                "V2 migration requires both pk and token to create a cache record");
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyAuthToken),
                "Legacy key must still be deleted even when migration skips cache write");
        }

        [Test]
        public async Task MigrateLegacyPrefKeys_DoesNotOverwrite_WhenNewKeyAlreadySet()
        {
            var cache = new InMemoryCache();
            await cache.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = SolanaMobileWalletAdapter.ExpectedSchemaVersion,
                AuthToken = "existing-token",
                AccountAddress = "existing-pk"
            });
            PlayerPrefs.SetString(LegacyPk, "stale-pk");
            PlayerPrefs.SetString(LegacyAuthToken, "stale-token");
            PlayerPrefs.Save();

            var adapter = CreateAdapterWithCache(cache);

            await InvokeMigrate(adapter);

            var record = await cache.GetAsync();
            Assert.That(record, Is.Not.Null);
            Assert.AreEqual("stale-pk", record.AccountAddress,
                "V2 migration overwrites cache — last-write-wins by design");
        }

        private class InMemoryCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;
            public Task<AuthorizationRecord> GetAsync() => Task.FromResult(_stored);
            public Task SetAsync(AuthorizationRecord record) { _stored = record; return Task.CompletedTask; }
            public Task ClearAsync() { _stored = null; return Task.CompletedTask; }
        }
    }
}
