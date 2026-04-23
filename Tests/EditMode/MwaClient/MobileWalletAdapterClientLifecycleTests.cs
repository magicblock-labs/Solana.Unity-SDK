using NUnit.Framework;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        /// Reads the last captured message as raw JSON so the tests assert the
        /// actual wire format rather than a round-trip through JsonRequest.
        /// </summary>
        private JObject DecodeLastRequestObject()
        {
            Assert.IsNotNull(_sender.LastMessage, "No message was sent to MockMessageSender");
            var json = Encoding.UTF8.GetString(_sender.LastMessage);
            return JObject.Parse(json);
        }

        private JObject DecodeRequestObjectAt(int index)
        {
            var json = Encoding.UTF8.GetString(_sender.SentMessages[index]);
            return JObject.Parse(json);
        }

        private JObject DecodeLastParamsObject()
        {
            return GetParamsObject(DecodeLastRequestObject());
        }

        private static JObject GetParamsObject(JObject request)
        {
            var paramsToken = request["params"];
            Assert.IsNotNull(paramsToken, "Request must include a params object");
            Assert.AreEqual(JTokenType.Object, paramsToken.Type,
                "Request params must serialize as a JSON object");
            return (JObject)paramsToken;
        }

        private static int GetRequestId(JObject request)
        {
            var idToken = request["id"];
            Assert.IsNotNull(idToken, "Request must include an id");
            Assert.AreEqual(JTokenType.Integer, idToken.Type,
                "Request id must serialize as a JSON integer");
            return idToken.Value<int>();
        }

        
        // Deauthorize request shape
        [Test]
        public void Deauthorize_SendsJsonRpc_WithCorrectMethod()
        {
            // Act
            _ = _client.Deauthorize("test-auth-token-abc123");

            // Assert
            var request = DecodeLastRequestObject();
            Assert.AreEqual("deauthorize", request.Value<string>("method"),
                "Method must be 'deauthorize'");
        }

        [Test]
        public void Deauthorize_SendsJsonRpc_WithVersion2_0()
        {
            _ = _client.Deauthorize("test-auth-token-abc123");

            var request = DecodeLastRequestObject();
            Assert.AreEqual("2.0", request.Value<string>("jsonrpc"),
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
            var paramsObject = DecodeLastParamsObject();
            Assert.AreEqual(authToken, paramsObject.Value<string>("auth_token"),
                "params.auth_token must match the supplied authToken");
        }

        [Test]
        public void Deauthorize_Params_DoesNotIncludeIdentity()
        {
            // Deauthorize is scoped to a token; identity fields are not part
            // of the MWA spec for this RPC and must not leak into the payload.
            _ = _client.Deauthorize("auth-token");

            var paramsObject = DecodeLastParamsObject();
            Assert.IsNull(paramsObject.Property("identity"),
                "Deauthorize must not send an identity key in params");
        }

        [Test]
        public void Deauthorize_Params_DoesNotIncludeCluster()
        {
            _ = _client.Deauthorize("auth-token");

            var paramsObject = DecodeLastParamsObject();
            Assert.IsNull(paramsObject.Property("cluster"),
                "Deauthorize must not send a cluster key in params");
        }

        [Test]
        public void Deauthorize_Params_DoesNotIncludePayloadsOrAddresses()
        {
            _ = _client.Deauthorize("auth-token");

            var paramsObject = DecodeLastParamsObject();
            Assert.IsNull(paramsObject.Property("payloads"),
                "Deauthorize must not send a payloads key in params");
            Assert.IsNull(paramsObject.Property("addresses"),
                "Deauthorize must not send an addresses key in params");
        }

        [Test]
        public void Deauthorize_MessageId_IsPositive()
        {
            _ = _client.Deauthorize("auth-token");

            var request = DecodeLastRequestObject();
            Assert.Greater(GetRequestId(request), 0,
                "Request Id must be a positive integer");
        }

        [Test]
        public void Deauthorize_MessageIds_AreIncrementing()
        {
            // Act - fire two requests
            _ = _client.Deauthorize("token-1");
            _ = _client.Deauthorize("token-2");

            var first = DecodeRequestObjectAt(0);
            var second = DecodeRequestObjectAt(1);

            // Assert
            Assert.AreEqual(GetRequestId(first) + 1, GetRequestId(second),
                "Each successive Deauthorize must have Id one greater than the previous");
        }

        [Test]
        public void Deauthorize_DoesNotThrow_WhenAuthToken_IsNull()
        {
            // The client should defer validation to the wallet; a null token
            // must still produce a well-formed request on the wire.
            Assert.DoesNotThrow(() => _client.Deauthorize(null),
                "Deauthorize must not throw when authToken is null");

            var request = DecodeLastRequestObject();
            var paramsObject = DecodeLastParamsObject();
            Assert.AreEqual("deauthorize", request.Value<string>("method"));
            Assert.IsNull(paramsObject.Property("auth_token"),
                "Null auth tokens must be omitted from params");
        }

        
        // GetCapabilities request shape
        [Test]
        public void GetCapabilities_SendsJsonRpc_WithCorrectMethod()
        {
            _ = _client.GetCapabilities();

            var request = DecodeLastRequestObject();
            Assert.AreEqual("get_capabilities", request.Value<string>("method"),
                "Method must be 'get_capabilities' (snake_case per MWA spec)");
        }

        [Test]
        public void GetCapabilities_SendsJsonRpc_WithVersion2_0()
        {
            _ = _client.GetCapabilities();

            var request = DecodeLastRequestObject();
            Assert.AreEqual("2.0", request.Value<string>("jsonrpc"));
        }

        [Test]
        public void GetCapabilities_SendsEmptyParams_NotNull()
        {
            // The implementation sends new JsonRequestParams() (empty object)
            // rather than null. Pin that so a refactor that flips it to null
            // (which some MWA servers reject) is caught here.
            _ = _client.GetCapabilities();

            var paramsObject = DecodeLastParamsObject();
            Assert.IsFalse(paramsObject.HasValues,
                "GetCapabilities must send an empty params object");
        }

        [Test]
        public void GetCapabilities_MessageId_IsPositive()
        {
            _ = _client.GetCapabilities();

            var request = DecodeLastRequestObject();
            Assert.Greater(GetRequestId(request), 0,
                "Request Id must be a positive integer");
        }

        [Test]
        public void GetCapabilities_MessageIds_AreIncrementing()
        {
            _ = _client.GetCapabilities();
            _ = _client.GetCapabilities();

            var first = DecodeRequestObjectAt(0);
            var second = DecodeRequestObjectAt(1);

            Assert.AreEqual(GetRequestId(first) + 1, GetRequestId(second),
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

            var first = DecodeRequestObjectAt(0);
            var second = DecodeRequestObjectAt(1);
            var third = DecodeRequestObjectAt(2);

            // Assert, the client maintains a single monotonic id counter.
            Assert.AreEqual(GetRequestId(first) + 1, GetRequestId(second),
                "Deauthorize id must follow Authorize id by 1");
            Assert.AreEqual(GetRequestId(second) + 1, GetRequestId(third),
                "GetCapabilities id must follow Deauthorize id by 1");
        }
    }
}
