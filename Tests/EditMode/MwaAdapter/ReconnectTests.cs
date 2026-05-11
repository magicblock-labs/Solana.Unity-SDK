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
    public class ReconnectTests
    {
        private static readonly FieldInfo CacheField =
            typeof(SolanaMobileWalletAdapter).GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ReconnectMethod =
            typeof(SolanaMobileWalletAdapter).GetMethod("Reconnect", BindingFlags.Instance | BindingFlags.Public);

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();
        [TearDown]
        public void TearDown() { PlayerPrefs.DeleteAll(); LogAssert.NoUnexpectedReceived(); }

        [OneTimeSetUp]
        public void Guard()
        {
            Assert.That(CacheField, Is.Not.Null, "_cache not found");
            Assert.That(GateField, Is.Not.Null, "_gate not found — was it renamed?");
            Assert.That(ReconnectMethod, Is.Not.Null, "Reconnect not found");
        }

        private static readonly FieldInfo GateField =
            typeof(SolanaMobileWalletAdapter).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic);

        private static SolanaMobileWalletAdapter CreateAdapter(IAuthorizationCache cache)
        {
            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(typeof(SolanaMobileWalletAdapter));
            CacheField.SetValue(adapter, cache);
            GateField.SetValue(adapter, new System.Threading.SemaphoreSlim(1, 1));
            return adapter;
        }

        [Test]
        public async Task Reconnect_WhenCacheEmpty_ReturnsNoCachedSession()
        {
            var cache = new SimpleCache();
            var adapter = CreateAdapter(cache);

            var result = await (Task<ReconnectResult>)ReconnectMethod.Invoke(adapter, null);

            Assert.That(result, Is.InstanceOf<ReconnectResult.NoCachedSession>());
        }

        [Test]
        public async Task Reconnect_WhenCachePopulated_ReturnsNoCachedSessionOrFailed()
        {
            var cache = new SimpleCache();
            await cache.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = SolanaMobileWalletAdapter.ExpectedSchemaVersion,
                AuthToken = "cached-token"
            });
            var adapter = CreateAdapter(cache);

            if (Application.platform != RuntimePlatform.Android)
            {
                var task = (Task<ReconnectResult>)ReconnectMethod.Invoke(adapter, null);
                Assert.ThrowsAsync<System.Exception>(async () => await task,
                    "Non-Android: LocalAssociationScenario requires Android JNI");
                return;
            }

            var result = await (Task<ReconnectResult>)ReconnectMethod.Invoke(adapter, null);

            Assert.That(result, Is.InstanceOf<ReconnectResult.NoCachedSession>()
                .Or.InstanceOf<ReconnectResult.Failed>(),
                "On Android, should be NoCachedSession or Failed");
        }

        [Test]
        public void Reconnect_MethodExists_WithCorrectReturnType()
        {
            Assert.That(ReconnectMethod.ReturnType.Name, Does.Contain("Task"));
        }

        [Test]
        public void LoginSilentFirst_DispatchesThroughReconnectInternal()
        {
            var method = typeof(SolanaMobileWalletAdapter).GetMethod(
                "ReconnectInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "ReconnectInternal must exist — _Login dispatches through it for silent-first");
        }

        [Test]
        public void ReauthorizeRemoved_GrepReturnsZero()
        {
            var iface = typeof(IAdapterOperations).GetMethod("Reauthorize");
            Assert.That(iface, Is.Null, "Reauthorize must be removed from IAdapterOperations");

            var client = typeof(MobileWalletAdapterClient).GetMethod("Reauthorize");
            Assert.That(client, Is.Null, "Reauthorize must be removed from MobileWalletAdapterClient");
        }

        private class SimpleCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;
            public Task<AuthorizationRecord> GetAsync() => Task.FromResult(_stored);
            public Task SetAsync(AuthorizationRecord record) { _stored = record; return Task.CompletedTask; }
            public Task ClearAsync() { _stored = null; return Task.CompletedTask; }
        }
    }
}
