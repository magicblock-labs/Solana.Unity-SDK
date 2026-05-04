using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;

namespace SolanaMobileStack.Tests.EditMode
{
    public class CustomCacheInjectionTest
    {
        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        [Test]
        public async Task CustomCache_RoutesAllCallsThroughInjectedImpl()
        {
            var custom = new InMemoryCache();
            await custom.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = SolanaMobileWalletAdapter.ExpectedSchemaVersion,
                AuthToken = "injected-token"
            });

            var options = new SolanaMobileWalletAdapterOptions { Cache = custom };

            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(
                typeof(SolanaMobileWalletAdapter));
            var cacheField = typeof(SolanaMobileWalletAdapter)
                .GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(cacheField, Is.Not.Null, "_cache field not found — was it renamed?");
            cacheField.SetValue(adapter, options.Cache);

            var method = typeof(SolanaMobileWalletAdapter).GetMethod(
                "LoadValidCachedRecordAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "LoadValidCachedRecordAsync not found — was it renamed?");
            var result = await (Task<AuthorizationRecord>)method.Invoke(adapter, null);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AuthToken, Is.EqualTo("injected-token"));
            Assert.That(custom.GetCallCount, Is.GreaterThanOrEqualTo(1));
        }

        private class InMemoryCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;
            public int GetCallCount { get; private set; }
            public int SetCallCount { get; private set; }
            public int ClearCallCount { get; private set; }

            public Task<AuthorizationRecord> GetAsync()
            {
                GetCallCount++;
                return Task.FromResult(_stored);
            }

            public Task SetAsync(AuthorizationRecord record)
            {
                SetCallCount++;
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
    }
}
