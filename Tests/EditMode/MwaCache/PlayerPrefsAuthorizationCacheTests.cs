using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolanaMobileStack.Tests.EditMode
{
    public class PlayerPrefsAuthorizationCacheTests
    {
        private const string CacheKey = "SolanaUnity.MWA.AuthorizationRecord.v1";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();
            typeof(PlayerPrefsAuthorizationCache)
                .GetField("_warnedThisSession", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, false);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            LogAssert.NoUnexpectedReceived();
        }

        private static AuthorizationRecord MakeRecord(string token = "test-token", int schema = 1) =>
            new AuthorizationRecord
            {
                SchemaVersion       = schema,
                AuthToken           = token,
                AccountAddress      = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
                AccountLabel        = "Test Label",
                AccountIcon         = null,
                Chains              = new[] { "solana:devnet" },
                Features            = new[] { "solana:signTransactions" },
                WalletUriBase       = "https://phantom.app",
                WalletIcon          = null,
                CachedAtUnixSeconds = 1700000000,
            };

        [Test]
        public async Task RoundTrip_PreservesRecord()
        {
            var cache = new PlayerPrefsAuthorizationCache();
            var expected = MakeRecord();

            await cache.SetAsync(expected);
            var actual = await cache.GetAsync();

            Assert.That(actual, Is.Not.Null);
            Assert.That(
                JsonConvert.SerializeObject(actual),
                Is.EqualTo(JsonConvert.SerializeObject(expected)));
        }

        [Test]
        public async Task GetAsync_OnMissingKey_ReturnsNull()
        {
            var cache = new PlayerPrefsAuthorizationCache();

            var result = await cache.GetAsync();

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAsync_OnCorruptJson_ReturnsNullAndDoesNotThrow()
        {
            PlayerPrefs.SetString(CacheKey, "{not valid JSON");
            PlayerPrefs.Save();

            LogAssert.Expect(LogType.Warning, new Regex("failed to deserialize"));

            var cache = new PlayerPrefsAuthorizationCache();
            AuthorizationRecord result = null;

            Assert.DoesNotThrow(() => { result = cache.GetAsync().GetAwaiter().GetResult(); });
            Assert.That(result, Is.Null);
        }

        [Test]
        public void LegacyPk_DeletedOnConstruction()
        {
            PlayerPrefs.SetString("pk", "legacy-value");
            PlayerPrefs.Save();
            Assert.That(PlayerPrefs.HasKey("pk"), Is.True);

            _ = new PlayerPrefsAuthorizationCache();

            Assert.That(PlayerPrefs.HasKey("pk"), Is.False);
        }

        [Test]
        public async Task ClearAsync_IsIdempotent_OnEmpty()
        {
            var cache = new PlayerPrefsAuthorizationCache();

            Assert.DoesNotThrowAsync(async () => await cache.ClearAsync());
            Assert.DoesNotThrowAsync(async () => await cache.ClearAsync());
        }

        [Test]
        public async Task SetAsync_OverwritesPriorValue()
        {
            var cache = new PlayerPrefsAuthorizationCache();

            await cache.SetAsync(MakeRecord("token-A"));
            await cache.SetAsync(MakeRecord("token-B"));
            var result = await cache.GetAsync();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AuthToken, Is.EqualTo("token-B"));
        }

        [Test]
        public void SetAsync_ThrowsOnNull()
        {
            var cache = new PlayerPrefsAuthorizationCache();

            Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync(null));
        }
    }
}
