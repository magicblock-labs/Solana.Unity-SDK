using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Contracts
{
    /// <summary>
    /// Compile-time / reflection contract tests for <see cref="IAdapterOperations"/>.
    /// These exist because the interface is the stable boundary between the
    /// SDK and every wallet transport that implements it. A silent signature
    /// drift here would break implementers without any compile error on our
    /// side until runtime, so we pin shape, parameter types, return types,
    /// and the <see cref="PreserveAttribute"/> (needed so IL2CPP / Unity
    /// managed stripping cannot remove these members in AOT builds).
    /// </summary>
    [Category("Lifecycle")]
    public class IAdapterOperationsContractTests
    {
        private static MethodInfo GetMethod(string name)
        {
            return typeof(IAdapterOperations)
                .GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool HasParams(MethodInfo m, params Type[] expected)
        {
            var actual = m.GetParameters().Select(p => p.ParameterType).ToArray();
            if (actual.Length != expected.Length) return false;
            for (int i = 0; i < actual.Length; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }

        
        // Authorize
        [Test]
        public void Interface_Has_Authorize_WithExpectedSignature()
        {
            var method = GetMethod(nameof(IAdapterOperations.Authorize));

            Assert.IsNotNull(method, "IAdapterOperations.Authorize must exist");
            Assert.AreEqual(typeof(Task<AuthorizationResult>), method.ReturnType,
                "Authorize must return Task<AuthorizationResult>");
            Assert.IsTrue(HasParams(method, typeof(Uri), typeof(Uri), typeof(string), typeof(string)),
                "Authorize params must be (Uri identityUri, Uri iconUri, string identityName, string rpcCluster)");
        }

        
        // Reauthorize
        [Test]
        public void Interface_Has_Reauthorize_WithExpectedSignature()
        {
            var method = GetMethod(nameof(IAdapterOperations.Reauthorize));

            Assert.IsNotNull(method, "IAdapterOperations.Reauthorize must exist");
            Assert.AreEqual(typeof(Task<AuthorizationResult>), method.ReturnType,
                "Reauthorize must return Task<AuthorizationResult>");
            Assert.IsTrue(HasParams(method, typeof(Uri), typeof(Uri), typeof(string), typeof(string)),
                "Reauthorize params must be (Uri identityUri, Uri iconUri, string identityName, string authToken)");
        }

        
        // Deauthorize
        [Test]
        public void Interface_Has_Deauthorize_AcceptingStringAuthToken()
        {
            var method = GetMethod(nameof(IAdapterOperations.Deauthorize));

            Assert.IsNotNull(method, "IAdapterOperations.Deauthorize must exist");
            Assert.IsTrue(HasParams(method, typeof(string)),
                "Deauthorize must accept exactly (string authToken)");
        }

        [Test]
        public void Deauthorize_ReturnType_IsNonGenericTask()
        {
            var method = GetMethod(nameof(IAdapterOperations.Deauthorize));

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(Task), method.ReturnType,
                "Deauthorize must return non-generic Task (fire-and-forget result)");
            Assert.IsFalse(method.ReturnType.IsGenericType,
                "Deauthorize return type must not be generic");
        }

        
        // GetCapabilities
        [Test]
        public void Interface_Has_GetCapabilities_WithNoParameters()
        {
            var method = GetMethod(nameof(IAdapterOperations.GetCapabilities));

            Assert.IsNotNull(method, "IAdapterOperations.GetCapabilities must exist");
            Assert.AreEqual(0, method.GetParameters().Length,
                "GetCapabilities must take no parameters");
        }

        [Test]
        public void GetCapabilities_ReturnType_IsTaskOfCapabilitiesResult()
        {
            var method = GetMethod(nameof(IAdapterOperations.GetCapabilities));

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(Task<CapabilitiesResult>), method.ReturnType,
                "GetCapabilities must return Task<CapabilitiesResult>");
        }

        
        // Sign operations, signatures unchanged but guarded against regression
        [Test]
        public void Interface_Has_SignTransactions_WithExpectedSignature()
        {
            var method = GetMethod(nameof(IAdapterOperations.SignTransactions));

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(Task<SignedResult>), method.ReturnType,
                "SignTransactions must return Task<SignedResult>");
            Assert.IsTrue(HasParams(method, typeof(IEnumerable<byte[]>)),
                "SignTransactions params must be (IEnumerable<byte[]> transactions)");
        }

        [Test]
        public void Interface_Has_SignMessages_WithExpectedSignature()
        {
            var method = GetMethod(nameof(IAdapterOperations.SignMessages));

            Assert.IsNotNull(method);
            Assert.AreEqual(typeof(Task<SignedResult>), method.ReturnType,
                "SignMessages must return Task<SignedResult>");
            Assert.IsTrue(HasParams(method, typeof(IEnumerable<byte[]>), typeof(IEnumerable<byte[]>)),
                "SignMessages params must be (IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses)");
        }

        
        // [Preserve] attribute coverage, required for IL2CPP / managed stripping
        [Test]
        public void AllInterfaceMethods_Have_PreserveAttribute()
        {
            var methods = typeof(IAdapterOperations)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public);

            Assert.Greater(methods.Length, 0, "Interface must expose at least one method");
            foreach (var method in methods)
            {
                var preserve = method.GetCustomAttribute<PreserveAttribute>();
                Assert.IsNotNull(preserve,
                    $"{method.Name} must carry [Preserve] so IL2CPP does not strip it in AOT builds");
            }
        }

        [Test]
        public void Interface_Itself_HasPreserveAttribute()
        {
            var preserve = typeof(IAdapterOperations).GetCustomAttribute<PreserveAttribute>();
            Assert.IsNotNull(preserve,
                "IAdapterOperations type must carry [Preserve]");
        }

        
        // Implementation sanity
        [Test]
        public void MobileWalletAdapterClient_Implements_IAdapterOperations()
        {
            // Cheap sanity check: the production implementer must still
            // satisfy the interface after any refactor.
            Assert.IsTrue(typeof(IAdapterOperations).IsAssignableFrom(typeof(MobileWalletAdapterClient)),
                "MobileWalletAdapterClient must implement IAdapterOperations");
        }

        [Test]
        public void InterfaceMethodCount_MatchesExpectedSurface()
        {
            // Deauthorize, GetCapabilities, SignTransactions, SignMessages.
            // If this number changes, the contract tests above must be
            // updated to cover any new members.
            var methods = typeof(IAdapterOperations)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public);

            Assert.AreEqual(6, methods.Length,
                "IAdapterOperations must expose exactly 6 methods; update contract tests when this changes");
        }
    }
}
