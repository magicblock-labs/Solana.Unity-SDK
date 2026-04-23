using NUnit.Framework;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Solana.Unity.SDK.Tests.EditMode.Mocks;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.MwaClient
{
    /// <summary>
    /// Edit mode tests for the lifecycle RPCs added in PR #269:
    /// <c>Deauthorize(authToken)</c> and <c>GetCapabilities()</c>.
    /// A mock sender captures the serialized JSON so tests can assert
    /// the exact wire shape, method name, id sequencing, and param
    /// whitelist without spinning up a real MWA session.
    /// </summary>
    [Category("Lifecycle")]
    public class MobileWalletAdapterClientLifecycleTests
    {
        private MockMessageSender _sender;
        private MobileWalletAdapterClient _client;

        [SetUp]
        public void SetUp()
        {
            _sender = new MockMessageSender();
            _client = new MobileWalletAdapterClient(_sender);
        }

        
        // Helpers

        /// <summary>
        /// Reads the last captured message and decodes it as JsonRequest.
        /// Mirrors the helper in MobileWalletAdapterClientTests.cs.
        /// </summary>
        private JsonRequest DecodeLastRequest()
        {
            Assert.IsNotNull(_sender.LastMessage, "No message was sent to MockMessageSender");
            var json = Encoding.UTF8.GetString(_sender.LastMessage);
            return JsonConvert.DeserializeObject<JsonRequest>(json);
        }

        private JsonRequest DecodeRequestAt(int index)
        {
            var json = Encoding.UTF8.GetString(_sender.SentMessages[index]);
            return JsonConvert.DeserializeObject<JsonRequest>(json);
        }

        
        // Deauthorize request shape
        [Test]
        public void Deauthorize_SendsJsonRpc_WithCorrectMethod()
        {
            // Act
            _ = _client.Deauthorize("test-auth-token-abc123");

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual("deauthorize", request.Method,
                "Method must be 'deauthorize'");
        }

        [Test]
        public void Deauthorize_SendsJsonRpc_WithVersion2_0()
        {
            _ = _client.Deauthorize("test-auth-token-abc123");

            var request = DecodeLastRequest();
            Assert.AreEqual("2.0", request.JsonRpc,
                "JsonRpc version must be '2.0'");
        }

        [Test]
        public void Deauthorize_SendsAuthToken_InParams()
        {
            // Arrange
            const string authToken = "auth-token-xyz-789";

            // Act
            _ = _client.Deauthorize(authToken);

            // Assert
            var request = DecodeLastRequest();
            Assert.IsNotNull(request.Params, "Params must not be null");
            Assert.AreEqual(authToken, request.Params.AuthToken,
                "Params.AuthToken must match the supplied authToken");
        }

        [Test]
        public void Deauthorize_Params_DoesNotIncludeIdentity()
        {
            // Deauthorize is scoped to a token; identity fields are not part
            // of the MWA spec for this RPC and must not leak into the payload.
            _ = _client.Deauthorize("auth-token");

            var request = DecodeLastRequest();
            Assert.IsNull(request.Params.Identity,
                "Deauthorize must not send an Identity block (uri/icon/name)");
        }

        [Test]
        public void Deauthorize_Params_DoesNotIncludeCluster()
        {
            _ = _client.Deauthorize("auth-token");

            var request = DecodeLastRequest();
            Assert.IsNull(request.Params.Cluster,
                "Deauthorize must not send a Cluster field");
        }

        [Test]
        public void Deauthorize_Params_DoesNotIncludePayloadsOrAddresses()
        {
            _ = _client.Deauthorize("auth-token");

            var request = DecodeLastRequest();
            Assert.IsNull(request.Params.Payloads,
                "Deauthorize must not send Payloads");
            Assert.IsNull(request.Params.Addresses,
                "Deauthorize must not send Addresses");
        }

        [Test]
        public void Deauthorize_MessageId_IsPositive()
        {
            _ = _client.Deauthorize("auth-token");

            var request = DecodeLastRequest();
            Assert.Greater(request.Id, 0, "Request Id must be a positive integer");
        }

        [Test]
        public void Deauthorize_MessageIds_AreIncrementing()
        {
            // Act - fire two requests
            _ = _client.Deauthorize("token-1");
            _ = _client.Deauthorize("token-2");

            var first = DecodeRequestAt(0);
            var second = DecodeRequestAt(1);

            // Assert
            Assert.AreEqual(first.Id + 1, second.Id,
                "Each successive Deauthorize must have Id one greater than the previous");
        }

        [Test]
        public void Deauthorize_DoesNotThrow_WhenAuthToken_IsNull()
        {
            // The client should defer validation to the wallet; a null token
            // must still produce a well-formed request on the wire.
            Assert.DoesNotThrow(() => _client.Deauthorize(null),
                "Deauthorize must not throw when authToken is null");

            var request = DecodeLastRequest();
            Assert.AreEqual("deauthorize", request.Method);
        }

        
        // GetCapabilities request shape
        [Test]
        public void GetCapabilities_SendsJsonRpc_WithCorrectMethod()
        {
            _ = _client.GetCapabilities();

            var request = DecodeLastRequest();
            Assert.AreEqual("get_capabilities", request.Method,
                "Method must be 'get_capabilities' (snake_case per MWA spec)");
        }

        [Test]
        public void GetCapabilities_SendsJsonRpc_WithVersion2_0()
        {
            _ = _client.GetCapabilities();

            var request = DecodeLastRequest();
            Assert.AreEqual("2.0", request.JsonRpc);
        }

        [Test]
        public void GetCapabilities_SendsEmptyParams_NotNull()
        {
            // The implementation sends new JsonRequestParams() (empty object)
            // rather than null. Pin that so a refactor that flips it to null
            // (which some MWA servers reject) is caught here.
            _ = _client.GetCapabilities();

            var request = DecodeLastRequest();
            Assert.IsNotNull(request.Params,
                "GetCapabilities must send a non-null (empty) Params object");
            Assert.IsNull(request.Params.Identity, "Params.Identity must be null");
            Assert.IsNull(request.Params.Cluster, "Params.Cluster must be null");
            Assert.IsNull(request.Params.AuthToken, "Params.AuthToken must be null");
            Assert.IsNull(request.Params.Payloads, "Params.Payloads must be null");
            Assert.IsNull(request.Params.Addresses, "Params.Addresses must be null");
        }

        [Test]
        public void GetCapabilities_MessageId_IsPositive()
        {
            _ = _client.GetCapabilities();

            var request = DecodeLastRequest();
            Assert.Greater(request.Id, 0, "Request Id must be a positive integer");
        }

        [Test]
        public void GetCapabilities_MessageIds_AreIncrementing()
        {
            _ = _client.GetCapabilities();
            _ = _client.GetCapabilities();

            var first = DecodeRequestAt(0);
            var second = DecodeRequestAt(1);

            Assert.AreEqual(first.Id + 1, second.Id,
                "Each successive GetCapabilities must have Id one greater than the previous");
        }

        [Test]
        public void GetCapabilities_ReturnType_IsTaskOfCapabilitiesResult()
        {
            // Locks the public return type so downstream callers do not
            // silently break if the method signature changes.
            var method = typeof(MobileWalletAdapterClient)
                .GetMethod(nameof(MobileWalletAdapterClient.GetCapabilities),
                    BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(method, "MobileWalletAdapterClient.GetCapabilities must exist");
            Assert.AreEqual(typeof(Task<CapabilitiesResult>), method.ReturnType,
                "GetCapabilities must return Task<CapabilitiesResult>");
        }

        
        // Cross-method id sequence
        [Test]
        public void MixedCalls_ShareMessageIdSequence()
        {
            // Arrange
            var identityUri = new System.Uri("https://example.com");

            // Act, intermix three different RPCs
            _ = _client.Authorize(identityUri, null, "TestApp", "mainnet-beta");
            _ = _client.Deauthorize("auth-token");
            _ = _client.GetCapabilities();

            var first = DecodeRequestAt(0);
            var second = DecodeRequestAt(1);
            var third = DecodeRequestAt(2);

            // Assert, the client maintains a single monotonic id counter.
            Assert.AreEqual(first.Id + 1, second.Id,
                "Deauthorize id must follow Authorize id by 1");
            Assert.AreEqual(second.Id + 1, third.Id,
                "GetCapabilities id must follow Deauthorize id by 1");
        }
    }
}
