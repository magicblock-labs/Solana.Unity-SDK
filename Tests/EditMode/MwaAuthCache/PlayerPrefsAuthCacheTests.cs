using NUnit.Framework;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.MwaAuthCache
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerPrefsAuthCache"/> and the
    /// <see cref="IMwaAuthCache"/> wiring on both adapters.
    ///
    /// Tests must not stomp on a developer's real PlayerPrefs values, so
    /// any key we touch gets snapshotted in <see cref="SetUp"/> and restored
    /// in <see cref="TearDown"/>. Same convention as
    /// SolanaMobileWalletAdapterPrefsTests in PR #283.
    /// </summary>
    [TestFixture]
    [Category("MwaAuthCache")]
    public class PlayerPrefsAuthCacheTests
    {
        // Duplicated here on purpose. If someone renames
        // PlayerPrefsAuthCache.DefaultKey, the BC test below should fail and
        // force a deliberate review.
        private const string DefaultKey = "solana_sdk.mwa.auth_token";
        private const string ScopedSuffix = "phantom";
        private const string ScopedKey = DefaultKey + "." + ScopedSuffix;

        private bool _hadDefault;
        private string _savedDefault;
        private bool _hadScoped;
        private string _savedScoped;

        [SetUp]
        public void SetUp()
        {
            _hadDefault = PlayerPrefs.HasKey(DefaultKey);
            _savedDefault = _hadDefault ? PlayerPrefs.GetString(DefaultKey) : null;
            _hadScoped = PlayerPrefs.HasKey(ScopedKey);
            _savedScoped = _hadScoped ? PlayerPrefs.GetString(ScopedKey) : null;

            PlayerPrefs.DeleteKey(DefaultKey);
            PlayerPrefs.DeleteKey(ScopedKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(DefaultKey);
            PlayerPrefs.DeleteKey(ScopedKey);

            if (_hadDefault) PlayerPrefs.SetString(DefaultKey, _savedDefault);
            if (_hadScoped) PlayerPrefs.SetString(ScopedKey, _savedScoped);

            PlayerPrefs.Save();
        }

        // ---------- Get / Set / Clear semantics ----------

        [Test]
        public async Task Get_ReturnsNull_WhenKeyMissing()
        {
            var cache = new PlayerPrefsAuthCache();

            string token = await cache.Get();

            // Empty string would be indistinguishable from "fresh install"
            // for the SDK's _authToken.IsNullOrEmpty() checks, so the cache
            // contract requires null on miss.
            Assert.IsNull(token);
        }

        [Test]
        public async Task SetThenGet_RoundTripsToken()
        {
            var cache = new PlayerPrefsAuthCache();

            await cache.Set("token-abc-123");
            string read = await cache.Get();

            Assert.AreEqual("token-abc-123", read);
        }

        [Test]
        public async Task Set_PersistsToken_VisibleViaPlayerPrefs()
        {
            var cache = new PlayerPrefsAuthCache();

            await cache.Set("token-via-cache");

            // Direct PlayerPrefs read must see the same value. This is the
            // contract that lets PR #269 sessions survive the upgrade with
            // no migration step.
            Assert.AreEqual("token-via-cache",
                PlayerPrefs.GetString(DefaultKey, null));
        }

        [Test]
        public async Task Set_WithEmptyToken_IsNoOp()
        {
            var cache = new PlayerPrefsAuthCache();
            PlayerPrefs.SetString(DefaultKey, "preexisting");
            PlayerPrefs.Save();

            await cache.Set(string.Empty);

            // A stale callsite passing "" must not wipe a valid session.
            Assert.AreEqual("preexisting", PlayerPrefs.GetString(DefaultKey, null));
        }

        [Test]
        public async Task Set_WithNullToken_IsNoOp()
        {
            var cache = new PlayerPrefsAuthCache();
            PlayerPrefs.SetString(DefaultKey, "preexisting");
            PlayerPrefs.Save();

            await cache.Set(null);

            // Same reasoning as the empty-string case.
            Assert.AreEqual("preexisting", PlayerPrefs.GetString(DefaultKey, null));
        }

        [Test]
        public async Task Clear_RemovesPersistedToken()
        {
            var cache = new PlayerPrefsAuthCache();
            await cache.Set("to-be-cleared");

            await cache.Clear();

            Assert.IsFalse(PlayerPrefs.HasKey(DefaultKey));
            Assert.IsNull(await cache.Get());
        }

        [Test]
        public async Task Clear_OnEmptyCache_IsIdempotent()
        {
            var cache = new PlayerPrefsAuthCache();

            // Logout() and DisconnectWallet() can both call Clear in a row,
            // so back-to-back invocations on an empty cache must be safe.
            await cache.Clear();
            await cache.Clear();

            Assert.IsNull(await cache.Get());
        }

        // ---------- Scoping ----------

        [Test]
        public async Task ScopedCache_WritesToScopedKey()
        {
            var scoped = new PlayerPrefsAuthCache(ScopedSuffix);

            await scoped.Set("scoped-token");

            Assert.AreEqual("scoped-token", PlayerPrefs.GetString(ScopedKey, null));
            // Scoped writes must not leak into the unscoped default bucket.
            Assert.IsFalse(PlayerPrefs.HasKey(DefaultKey));
        }

        [Test]
        public async Task ScopedCache_DoesNotSeeUnscopedToken()
        {
            // Apps that connect to multiple wallets (e.g. Phantom + Solflare)
            // need each scope to be a separate bucket.
            var unscoped = new PlayerPrefsAuthCache();
            var scoped = new PlayerPrefsAuthCache(ScopedSuffix);
            await unscoped.Set("only-in-default");

            Assert.IsNull(await scoped.Get());
        }

        [Test]
        public async Task UnscopedCache_DoesNotSeeScopedToken()
        {
            var unscoped = new PlayerPrefsAuthCache();
            var scoped = new PlayerPrefsAuthCache(ScopedSuffix);
            await scoped.Set("only-in-scoped");

            Assert.IsNull(await unscoped.Get());
        }

        [Test]
        public void Constructor_WithEmptyScope_FallsBackToDefaultKey()
        {
            var emptyScope = new PlayerPrefsAuthCache(string.Empty);
            var nullScope = new PlayerPrefsAuthCache(null);
            var noArg = new PlayerPrefsAuthCache();

            string keyEmpty = GetPrivateKey(emptyScope);
            string keyNull = GetPrivateKey(nullScope);
            string keyNoArg = GetPrivateKey(noArg);

            Assert.AreEqual(DefaultKey, keyEmpty);
            Assert.AreEqual(DefaultKey, keyNull);
            Assert.AreEqual(DefaultKey, keyNoArg);
        }

        // ---------- Backward compatibility with PR #269 ----------

        [Test]
        public void DefaultKey_StaysOnPR269Constant()
        {
            // DefaultKey is the anchor for backward compatibility. If
            // someone changes its value, every existing install loses its
            // cached session on upgrade. That should never be a quiet
            // change, so we pin it here.
            FieldInfo field = typeof(PlayerPrefsAuthCache).GetField(
                "DefaultKey",
                BindingFlags.Public | BindingFlags.Static);

            Assert.IsNotNull(field);
            Assert.AreEqual(DefaultKey, field.GetRawConstantValue() as string);
        }

        [Test]
        public async Task PreExisting_PR269_TokenIsReadableByDefaultCache()
        {
            // Simulate an install that connected on main after PR #269
            // shipped, before this abstraction existed. Their token is
            // sitting at solana_sdk.mwa.auth_token. After upgrade, the
            // default cache must still find it - no migration ever runs.
            PlayerPrefs.SetString(DefaultKey, "session-from-pr269");
            PlayerPrefs.Save();

            var cache = new PlayerPrefsAuthCache();

            Assert.AreEqual("session-from-pr269", await cache.Get());
        }

        // ---------- Adapter wiring contract ----------
        //
        // These tests catch the case where a future refactor accidentally
        // drops the optional IMwaAuthCache parameter from either adapter
        // constructor. Without it, custom cache injection silently breaks
        // and there's no compile error to flag the regression.

        [Test]
        public void SolanaMobileWalletAdapter_AcceptsOptionalIMwaAuthCache()
        {
            var ctor = FindCtorWithAuthCacheParam(
                typeof(SolanaMobileWalletAdapter));

            Assert.IsNotNull(ctor);

            ParameterInfo authCacheParam = ctor.GetParameters()
                .First(p => p.ParameterType == typeof(IMwaAuthCache));
            // Optional + default null = backward compatible. Existing
            // call sites that don't pass authCache must still compile.
            Assert.IsTrue(authCacheParam.IsOptional);
            Assert.IsNull(authCacheParam.DefaultValue);
        }

        [Test]
        public void SolanaWalletAdapter_AcceptsOptionalIMwaAuthCache()
        {
            var ctor = FindCtorWithAuthCacheParam(typeof(SolanaWalletAdapter));

            Assert.IsNotNull(ctor);

            ParameterInfo authCacheParam = ctor.GetParameters()
                .First(p => p.ParameterType == typeof(IMwaAuthCache));
            Assert.IsTrue(authCacheParam.IsOptional);
            Assert.IsNull(authCacheParam.DefaultValue);
        }

        // ---------- Helpers ----------

        private static string GetPrivateKey(PlayerPrefsAuthCache cache)
        {
            // _key is the live storage contract for an instance. Renaming
            // it requires updating these tests.
            FieldInfo field = typeof(PlayerPrefsAuthCache).GetField(
                "_key",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);
            return (string)field.GetValue(cache);
        }

        private static ConstructorInfo FindCtorWithAuthCacheParam(System.Type t)
        {
            return t.GetConstructors()
                .FirstOrDefault(c => c.GetParameters()
                    .Any(p => p.ParameterType == typeof(IMwaAuthCache)));
        }
    }
}
