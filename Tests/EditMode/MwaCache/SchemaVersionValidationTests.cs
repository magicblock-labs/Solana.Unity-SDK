using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;

namespace SolanaMobileStack.Tests.EditMode
{
    public class SchemaVersionValidationTests
    {
        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        private static readonly MethodInfo LoadValidMethod =
            typeof(SolanaMobileWalletAdapter).GetMethod(
                "LoadValidCachedRecordAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CacheField =
            typeof(SolanaMobileWalletAdapter).GetField(
                "_cache", BindingFlags.Instance | BindingFlags.NonPublic);

        [OneTimeSetUp]
        public void GuardReflectionTargets()
        {
            Assert.That(LoadValidMethod, Is.Not.Null,
                "LoadValidCachedRecordAsync not found — was it renamed?");
            Assert.That(CacheField, Is.Not.Null,
                "_cache field not found — was it renamed?");
        }

        private static SolanaMobileWalletAdapter CreateAdapterWithCache(IAuthorizationCache cache)
        {
            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(
                typeof(SolanaMobileWalletAdapter));
            CacheField.SetValue(adapter, cache);
            return adapter;
        }

        private static async Task<AuthorizationRecord> InvokeLoadValid(SolanaMobileWalletAdapter adapter)
        {
            return await (Task<AuthorizationRecord>)LoadValidMethod.Invoke(adapter, null);
        }

        [Test]
        public async Task LoadValidCachedRecordAsync_ReturnsNull_WhenSchemaMismatch()
        {
            var cache = new SimpleCache();
            await cache.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = 99,
                AuthToken = "stale-token"
            });
            var adapter = CreateAdapterWithCache(cache);

            var result = await InvokeLoadValid(adapter);

            Assert.That(result, Is.Null);
            Assert.That(await cache.GetAsync(), Is.Null, "ClearAsync must have been called");
        }

        [Test]
        public async Task LoadValidCachedRecordAsync_ReturnsRecord_WhenSchemaMatches()
        {
            var cache = new SimpleCache();
            await cache.SetAsync(new AuthorizationRecord
            {
                SchemaVersion = SolanaMobileWalletAdapter.ExpectedSchemaVersion,
                AuthToken = "valid-token"
            });
            var adapter = CreateAdapterWithCache(cache);

            var result = await InvokeLoadValid(adapter);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AuthToken, Is.EqualTo("valid-token"));
        }

        [Test]
        public async Task LoadValidCachedRecordAsync_ReturnsNull_WhenCacheEmpty()
        {
            var cache = new SimpleCache();
            var adapter = CreateAdapterWithCache(cache);

            var result = await InvokeLoadValid(adapter);

            Assert.That(result, Is.Null);
        }

        private class SimpleCache : IAuthorizationCache
        {
            private AuthorizationRecord _stored;

            public Task<AuthorizationRecord> GetAsync() =>
                Task.FromResult(_stored);

            public Task SetAsync(AuthorizationRecord record)
            {
                _stored = record;
                return Task.CompletedTask;
            }

            public Task ClearAsync()
            {
                _stored = null;
                return Task.CompletedTask;
            }
        }
    }
}
