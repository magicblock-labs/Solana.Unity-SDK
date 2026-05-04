using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolanaMobileStack.Tests.EditMode
{
    public class DeauthorizeTests
    {
        private static readonly FieldInfo CacheField =
            typeof(SolanaMobileWalletAdapter).GetField(
                "_cache", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo AuthTokenField =
            typeof(SolanaMobileWalletAdapter).GetField(
                "_authToken", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo DeauthorizeMethod =
            typeof(SolanaMobileWalletAdapter).GetMethod(
                "Deauthorize", BindingFlags.Instance | BindingFlags.Public);

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
            LogAssert.NoUnexpectedReceived();
        }

        [OneTimeSetUp]
        public void GuardReflectionTargets()
        {
            Assert.That(CacheField, Is.Not.Null, "_cache field not found — was it renamed?");
            Assert.That(AuthTokenField, Is.Not.Null, "_authToken field not found — was it renamed?");
            Assert.That(DeauthorizeMethod, Is.Not.Null, "Deauthorize method not found — was it renamed?");
        }

        private static SolanaMobileWalletAdapter CreateAdapter(IAuthorizationCache cache, string authToken = null)
        {
            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(
                typeof(SolanaMobileWalletAdapter));
            CacheField.SetValue(adapter, cache);
            if (authToken != null)
                AuthTokenField.SetValue(adapter, authToken);
            return adapter;
        }

        private static async Task<DeauthorizeResult> InvokeDeauthorize(SolanaMobileWalletAdapter adapter)
        {
            return await (Task<DeauthorizeResult>)DeauthorizeMethod.Invoke(adapter, null);
        }

        [Test]
        public async Task Deauthorize_WhenCacheEmpty_ReturnsFullyRevoked()
        {
            var cache = new TestCache();
            var adapter = CreateAdapter(cache);

            var result = await InvokeDeauthorize(adapter);

            Assert.That(result, Is.InstanceOf<DeauthorizeResult.FullyRevoked>());
            Assert.That(cache.ClearCallCount, Is.EqualTo(0),
                "ClearAsync should NOT be called when cache is already empty");
        }

        [Test]
        public async Task Deauthorize_WhenCachePopulated_ClearsCache()
        {
            var cache = new TestCache();
            await cache.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = SolanaMobileWalletAdapter.ExpectedSchemaVersion,
                AuthToken = "test-token",
                WalletUriBase = "https://phantom.app"
            });
            var adapter = CreateAdapter(cache, authToken: "test-token");

            var result = await InvokeDeauthorize(adapter);

            Assert.That(result, Is.InstanceOf<DeauthorizeResult.FullyRevoked>()
                .Or.InstanceOf<DeauthorizeResult.LocalOnly>(),
                "Should be FullyRevoked (RPC success) or LocalOnly (RPC fail in EditMode)");
            Assert.That(cache.ClearCallCount, Is.GreaterThanOrEqualTo(1),
                "ClearAsync must be called on populated cache");
            Assert.That(await cache.GetAsync(), Is.Null,
                "Cache must be empty after deauthorize");
        }

        [Test]
        public async Task Deauthorize_WhenClearFails_ReturnsFailed()
        {
            var cache = new FailingClearCache();
            await cache.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = SolanaMobileWalletAdapter.ExpectedSchemaVersion,
                AuthToken = "doomed-token"
            });
            var adapter = CreateAdapter(cache, authToken: "doomed-token");

            PlayerPrefs.SetString("pk", "sentinel");
            PlayerPrefs.Save();

            var result = await InvokeDeauthorize(adapter);

            Assert.That(result, Is.InstanceOf<DeauthorizeResult.Failed>());
            var failed = (DeauthorizeResult.Failed)result;
            Assert.That(failed.Error, Is.Not.Null);
            Assert.That(PlayerPrefs.HasKey("pk"), Is.True,
                "Logout() must NOT fire on Failed path — pk sentinel should survive");
        }

        [Test]
        public async Task Deauthorize_ClearsAuthTokenField()
        {
            var cache = new TestCache();
            var adapter = CreateAdapter(cache, authToken: "active-token");
            Assert.That(AuthTokenField.GetValue(adapter), Is.EqualTo("active-token"));

            await InvokeDeauthorize(adapter);

            Assert.That(AuthTokenField.GetValue(adapter), Is.Null,
                "_authToken must be null after deauthorize");
        }

        private class TestCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;
            public int ClearCallCount { get; private set; }

            public Task<AuthorizationRecord> GetAsync() =>
                Task.FromResult(_stored);

            public Task SetAsync(AuthorizationRecord record)
            {
                _stored = record;
                return Task.CompletedTask;
            }

            public Task ClearAsync()
            {
                ClearCallCount++;
                _stored = null;
                return Task.CompletedTask;
            }
        }

        private class FailingClearCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;

            public Task<AuthorizationRecord> GetAsync() =>
                Task.FromResult(_stored);

            public Task SetAsync(AuthorizationRecord record)
            {
                _stored = record;
                return Task.CompletedTask;
            }

            public Task ClearAsync() =>
                throw new InvalidOperationException("Simulated cache clear failure");
        }
    }
}
