using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Framework;
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolanaMobileStack.Tests.EditMode
{
    public class ConcurrencyTests
    {
        [TearDown]
        public void TearDown() => LogAssert.NoUnexpectedReceived();

        [Test]
        public void Adapter_HasSemaphoreSlimGateField()
        {
            var field = typeof(SolanaMobileWalletAdapter).GetField(
                "_gate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "_gate field not found — was it renamed?");
            Assert.That(field.FieldType, Is.EqualTo(typeof(SemaphoreSlim)),
                "_gate must be SemaphoreSlim");

            var adapter = (SolanaMobileWalletAdapter)FormatterServices.GetUninitializedObject(
                typeof(SolanaMobileWalletAdapter));
            var gate = (SemaphoreSlim)field.GetValue(adapter);
            Assert.That(gate, Is.Not.Null, "_gate must be initialized (inline field initializer)");
            Assert.That(gate.CurrentCount, Is.EqualTo(1),
                "_gate must be SemaphoreSlim(1,1) — initial count 1");
        }

        [Test]
        public void Adapter_HasCurrentOperationField()
        {
            var field = typeof(SolanaMobileWalletAdapter).GetField(
                "_currentOperation", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "_currentOperation field not found — was it renamed?");
            Assert.That(field.FieldType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Adapter_HasSignAndSendTransactionsPublicMethod()
        {
            var method = typeof(SolanaMobileWalletAdapter).GetMethod(
                "SignAndSendTransactions", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null,
                "SignAndSendTransactions public method not found");
            Assert.That(method.ReturnType.Name, Does.Contain("Task"),
                "SignAndSendTransactions must return Task<SignAndSendTxResult>");
        }

        [Test]
        public void Adapter_HasSignAndSendTransactionOverride()
        {
            var method = typeof(SolanaMobileWalletAdapter).GetMethod(
                "SignAndSendTransaction", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null,
                "SignAndSendTransaction override not found");
            Assert.That(method.DeclaringType, Is.EqualTo(typeof(SolanaMobileWalletAdapter)),
                "SignAndSendTransaction must be declared (overridden) on SolanaMobileWalletAdapter, not inherited");
        }

        [Test]
        public void Adapter_HasSignAndSendTransactionsInternalPrivateMethod()
        {
            var method = typeof(SolanaMobileWalletAdapter).GetMethod(
                "SignAndSendTransactionsInternal",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "SignAndSendTransactionsInternal private method not found — gate pattern requires public/internal split (§10.3)");
        }
    }
}
