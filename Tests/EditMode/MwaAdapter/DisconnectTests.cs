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
    public class DisconnectTests
    {
        private static readonly FieldInfo CacheField =
            typeof(SolanaMobileWalletAdapter).GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AuthTokenField =
            typeof(SolanaMobileWalletAdapter).GetField("_authToken", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo DisconnectMethod =
            typeof(SolanaMobileWalletAdapter).GetMethod("Disconnect", BindingFlags.Instance | BindingFlags.Public);

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();
        [TearDown]
        public void TearDown() { PlayerPrefs.DeleteAll(); LogAssert.NoUnexpectedReceived(); }

        [OneTimeSetUp]
        public void Guard()
        {
            Assert.That(CacheField, Is.Not.Null, "_cache not found");
            Assert.That(DisconnectMethod, Is.Not.Null, "Disconnect not found");
        }

        [Test]
        public async Task Disconnect_ClearsCacheAndAuthToken()
        {
            var cache = new TestCache();
            await cache.SetAsync(new AuthorizationRecord { SchemaVersion = 1, AuthToken = "tok" });
            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(typeof(SolanaMobileWalletAdapter));
            CacheField.SetValue(adapter, cache);
            AuthTokenField.SetValue(adapter, "tok");

            // Disconnect uses gate — need to init _gate field
            var gateField = typeof(SolanaMobileWalletAdapter).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic);
            gateField.SetValue(adapter, new System.Threading.SemaphoreSlim(1, 1));

            await (Task)DisconnectMethod.Invoke(adapter, null);

            Assert.That(await cache.GetAsync(), Is.Null, "Cache must be cleared");
            Assert.That(cache.ClearCallCount, Is.EqualTo(1));
            Assert.That(AuthTokenField.GetValue(adapter), Is.Null, "_authToken must be null");
        }

        private class TestCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;
            public int ClearCallCount { get; private set; }
            public Task<AuthorizationRecord> GetAsync() => Task.FromResult(_stored);
            public Task SetAsync(AuthorizationRecord record) { _stored = record; return Task.CompletedTask; }
            public Task ClearAsync() { ClearCallCount++; _stored = null; return Task.CompletedTask; }
        }
    }
}
