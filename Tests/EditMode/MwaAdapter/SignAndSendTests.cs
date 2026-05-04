using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolanaMobileStack.Tests.EditMode
{
    public class SignAndSendTests
    {
        [TearDown]
        public void TearDown() => LogAssert.NoUnexpectedReceived();

        [Test]
        public void SignAndSendRequest_EmitsMethodAndPayloads()
        {
            var req = new JsonRequest
            {
                JsonRpc = "2.0",
                Method = "sign_and_send_transactions",
                Params = new JsonRequest.JsonRequestParams
                {
                    Payloads = new List<string> { "AQID", "BAUG" }
                },
                Id = 1
            };
            string json = JsonConvert.SerializeObject(req);

            StringAssert.Contains("\"method\":\"sign_and_send_transactions\"", json);
            StringAssert.Contains("\"payloads\":[\"AQID\",\"BAUG\"]", json);
            StringAssert.DoesNotContain("\"auth_token\"", json,
                "sign_and_send_transactions must NOT include auth_token (session-authorized)");
        }

        [Test]
        public void SignAndSendRequest_OmitsOptionsWhenNull()
        {
            var req = new JsonRequest
            {
                JsonRpc = "2.0",
                Method = "sign_and_send_transactions",
                Params = new JsonRequest.JsonRequestParams
                {
                    Payloads = new List<string> { "AQID" }
                },
                Id = 1
            };
            string json = JsonConvert.SerializeObject(req);

            StringAssert.DoesNotContain("\"options\"", json,
                "options must be omitted when null");
        }

        [Test]
        public void SignAndSendRequest_IncludesSnakeCaseOptions()
        {
            var wireOptions = new JObject
            {
                ["commitment"] = "confirmed",
                ["skip_preflight"] = false,
                ["max_retries"] = 3
            };
            var req = new JsonRequest
            {
                JsonRpc = "2.0",
                Method = "sign_and_send_transactions",
                Params = new JsonRequest.JsonRequestParams
                {
                    Payloads = new List<string> { "AQID" },
                    Options = wireOptions
                },
                Id = 1
            };
            string json = JsonConvert.SerializeObject(req);

            StringAssert.Contains("\"options\"", json);
            StringAssert.Contains("\"commitment\":\"confirmed\"", json);
            StringAssert.Contains("\"skip_preflight\":false", json);
            StringAssert.Contains("\"max_retries\":3", json);
        }

        [Test]
        public void LegacyTransactionPayload_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LegacyTransactionPayload(null));
        }

        [Test]
        public void LegacyTransactionPayload_ImplementsInterface()
        {
            Assert.That(typeof(ITransactionPayload).IsAssignableFrom(typeof(LegacyTransactionPayload)),
                "LegacyTransactionPayload must implement ITransactionPayload");
        }

        [Test]
        public void SignAndSendTxResult_HasAllSevenVariants()
        {
            Assert.That(typeof(SignAndSendTxResult.Success), Is.Not.Null);
            Assert.That(typeof(SignAndSendTxResult.UserDenied), Is.Not.Null);
            Assert.That(typeof(SignAndSendTxResult.InvalidPayloads), Is.Not.Null);
            Assert.That(typeof(SignAndSendTxResult.NotSubmitted), Is.Not.Null);
            Assert.That(typeof(SignAndSendTxResult.TooManyPayloads), Is.Not.Null);
            Assert.That(typeof(SignAndSendTxResult.AuthRevoked), Is.Not.Null);
            Assert.That(typeof(SignAndSendTxResult.WalletUnreachable), Is.Not.Null);
        }
    }
}
