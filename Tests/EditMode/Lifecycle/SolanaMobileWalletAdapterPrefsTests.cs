using System.Collections.Generic;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Lifecycle
{
    /// <summary>
    /// Edit mode tests for <see cref="SolanaMobileWalletAdapter"/> PlayerPrefs
    /// behavior introduced in PR #269.
    ///
    /// The adapter constructor throws on non-Android platforms, so we cannot
    /// instantiate the class in the Editor. Instead we use reflection to:
    ///   1. Pin the new namespaced key constants (PrefKeyPublicKey /
    ///      PrefKeyAuthToken) so a rename is caught immediately.
    ///   2. Invoke the private static <c>MigrateLegacyPrefKeys</c> method
    ///      and assert legacy <c>"pk"</c> / <c>"authToken"</c> entries move
    ///      to the namespaced keys exactly once without overwriting newer
    ///      data.
    ///
    /// [SetUp]/[TearDown] snapshot and restore any pre-existing values because
    /// EditMode PlayerPrefs persist in the Unity editor project between runs.
    /// </summary>
    [Category("Lifecycle")]
    public class SolanaMobileWalletAdapterPrefsTests
    {
        private const string LegacyPk = "pk";
        private const string LegacyAuthToken = "authToken";
        private const string NewPkKey = "solana_sdk.mwa.public_key";
        private const string NewAuthTokenKey = "solana_sdk.mwa.auth_token";
        private static readonly string[] RelevantKeys =
        {
            LegacyPk,
            LegacyAuthToken,
            NewPkKey,
            NewAuthTokenKey
        };

        private Dictionary<string, string> _originalPrefs;

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
            {
                if (PlayerPrefs.HasKey(key))
                {
                    snapshot[key] = PlayerPrefs.GetString(key);
                }
            }

            return snapshot;
        }

        private void RestoreOriginalKeys()
        {
            if (_originalPrefs == null)
            {
                return;
            }

            foreach (var entry in _originalPrefs)
            {
                PlayerPrefs.SetString(entry.Key, entry.Value);
            }

            PlayerPrefs.Save();
        }

        private static void DeleteAllRelevantKeys()
        {
            foreach (var key in RelevantKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        private static void InvokeMigrate()
        {
            // MigrateLegacyPrefKeys is private static; reflection is the only
            // way to exercise it without instantiating the adapter (which
            // fails on non-Android editors).
            var method = typeof(SolanaMobileWalletAdapter)
                .GetMethod("MigrateLegacyPrefKeys",
                    BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method,
                "Private static MigrateLegacyPrefKeys must exist on SolanaMobileWalletAdapter");
            method.Invoke(null, null);
        }

        private static string GetPrivateConst(string name)
        {
            var field = typeof(SolanaMobileWalletAdapter)
                .GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Private const {name} must exist");
            return (string)field.GetRawConstantValue();
        }

        
        // Pin the namespaced key values
        [Test]
        public void PrefKeyConstants_HaveNamespacedValues()
        {
            // These exact strings form the cross-version migration contract.
            // Changing them breaks existing installs that persisted the old
            // legacy keys through the migration step in the ctor.
            Assert.AreEqual(NewPkKey, GetPrivateConst("PrefKeyPublicKey"),
                "PrefKeyPublicKey must stay 'solana_sdk.mwa.public_key'");
            Assert.AreEqual(NewAuthTokenKey, GetPrivateConst("PrefKeyAuthToken"),
                "PrefKeyAuthToken must stay 'solana_sdk.mwa.auth_token'");
        }

        
        // Migration behavior
        [Test]
        public void MigrateLegacyPrefKeys_NoLegacyKeys_IsNoOp()
        {
            // Nothing in PlayerPrefs at start.
            InvokeMigrate();

            Assert.IsFalse(PlayerPrefs.HasKey(LegacyPk));
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyAuthToken));
            Assert.IsFalse(PlayerPrefs.HasKey(NewPkKey),
                "Migration must not invent data when nothing is stored");
            Assert.IsFalse(PlayerPrefs.HasKey(NewAuthTokenKey),
                "Migration must not invent data when nothing is stored");
        }

        [Test]
        public void MigrateLegacyPrefKeys_Migrates_LegacyPk_ToNewKey()
        {
            // Arrange
            PlayerPrefs.SetString(LegacyPk, "pubkey-abc");
            PlayerPrefs.Save();

            // Act
            InvokeMigrate();

            // Assert
            Assert.IsTrue(PlayerPrefs.HasKey(NewPkKey),
                "Legacy 'pk' must be copied to the namespaced key");
            Assert.AreEqual("pubkey-abc", PlayerPrefs.GetString(NewPkKey),
                "Namespaced key must carry the legacy value");
        }

        [Test]
        public void MigrateLegacyPrefKeys_Migrates_LegacyAuthToken_ToNewKey()
        {
            // Arrange
            PlayerPrefs.SetString(LegacyAuthToken, "token-xyz-123");
            PlayerPrefs.Save();

            // Act
            InvokeMigrate();

            // Assert
            Assert.IsTrue(PlayerPrefs.HasKey(NewAuthTokenKey),
                "Legacy 'authToken' must be copied to the namespaced key");
            Assert.AreEqual("token-xyz-123", PlayerPrefs.GetString(NewAuthTokenKey));
        }

        [Test]
        public void MigrateLegacyPrefKeys_Deletes_LegacyKeys_AfterMigration()
        {
            // Arrange
            PlayerPrefs.SetString(LegacyPk, "pubkey-abc");
            PlayerPrefs.SetString(LegacyAuthToken, "token-xyz-123");
            PlayerPrefs.Save();

            // Act
            InvokeMigrate();

            // Assert - legacy keys must be gone so a subsequent call is a no-op.
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyPk),
                "Legacy 'pk' key must be deleted after migration");
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyAuthToken),
                "Legacy 'authToken' key must be deleted after migration");
        }

        [Test]
        public void MigrateLegacyPrefKeys_DoesNotOverwrite_WhenNewKeyAlreadySet()
        {
            // If a newer session has already produced namespaced values,
            // the migration must not clobber them with stale legacy data.
            PlayerPrefs.SetString(LegacyPk, "legacy-pubkey");
            PlayerPrefs.SetString(NewPkKey, "new-pubkey");
            PlayerPrefs.SetString(LegacyAuthToken, "legacy-token");
            PlayerPrefs.SetString(NewAuthTokenKey, "new-token");
            PlayerPrefs.Save();

            // Act
            InvokeMigrate();

            // Assert
            Assert.AreEqual("new-pubkey", PlayerPrefs.GetString(NewPkKey),
                "Existing namespaced pubkey must not be overwritten");
            Assert.AreEqual("new-token", PlayerPrefs.GetString(NewAuthTokenKey),
                "Existing namespaced auth token must not be overwritten");
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyPk),
                "Legacy 'pk' key must still be deleted even when skipped");
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyAuthToken),
                "Legacy 'authToken' key must still be deleted even when skipped");
        }

        [Test]
        public void MigrateLegacyPrefKeys_SecondCall_IsNoOp()
        {
            // Idempotence: calling migrate twice in a row (e.g. two adapter
            // instances in the same session) must not corrupt data.
            PlayerPrefs.SetString(LegacyPk, "pubkey-abc");
            PlayerPrefs.Save();

            InvokeMigrate();
            InvokeMigrate();

            Assert.IsTrue(PlayerPrefs.HasKey(NewPkKey));
            Assert.AreEqual("pubkey-abc", PlayerPrefs.GetString(NewPkKey));
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyPk));
        }

        [Test]
        public void MigrateLegacyPrefKeys_OnlyAuthTokenPresent_Migrates()
        {
            // Half-migrated installs must still converge.
            PlayerPrefs.SetString(LegacyAuthToken, "only-token");
            PlayerPrefs.Save();

            InvokeMigrate();

            Assert.AreEqual("only-token", PlayerPrefs.GetString(NewAuthTokenKey));
            Assert.IsFalse(PlayerPrefs.HasKey(NewPkKey),
                "Pubkey namespaced key must not be fabricated when only auth token was legacy");
            Assert.IsFalse(PlayerPrefs.HasKey(LegacyAuthToken));
        }
    }
}
