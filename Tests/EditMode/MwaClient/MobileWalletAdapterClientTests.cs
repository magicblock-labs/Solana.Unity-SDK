using NUnit.Framework;
using System;
using System.Text;
using Newtonsoft.Json;
using Solana.Unity.SDK.Tests.EditMode.Mocks;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.MwaClient
{
    /// <summary>
    /// Edit mode tests for MobileWalletAdapterClient request building.
    /// A mock sender lets us inspect the JSON payloads without a real socket.
    /// </summary>
    public class MobileWalletAdapterClientTests
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
        /// Reads the last message the client sent and deserializes it as a JsonRequest.
        /// Right now these requests are still plain JSON at this stage of the flow.
        /// If that ever changes, this helper will need to be updated too.
        /// </summary>
        private JsonRequest DecodeLastRequest()
        {
            Assert.IsNotNull(_sender.LastMessage, "No message was sent to MockMessageSender");
            var json = Encoding.UTF8.GetString(_sender.LastMessage);
            return JsonConvert.DeserializeObject<JsonRequest>(json);
        }

       
        // Authorize request shape
        [Test]
        public void Authorize_SendsJsonRpc_WithCorrectMethod()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");
            var iconUri = new Uri("/icon.png", UriKind.Relative);
            const string identityName = "TestApp";
            const string cluster = "mainnet-beta";

            // Act
            _ = _client.Authorize(identityUri, iconUri, identityName, cluster);

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual("authorize", request.Method,
                "Method must be 'authorize'");
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithVersion2_0()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");

            // Act
            _ = _client.Authorize(identityUri, null, "TestApp", "mainnet-beta");

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual("2.0", request.JsonRpc,
                "JsonRpc version must be '2.0'");
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithNonZeroId()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");

            // Act
            _ = _client.Authorize(identityUri, null, "TestApp", "mainnet-beta");

            // Assert
            var request = DecodeLastRequest();
            Assert.Greater(request.Id, 0, "Request Id must be a positive integer");
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithIdentityName()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");
            const string identityName = "CrossyRoad";

            // Act
            _ = _client.Authorize(identityUri, null, identityName, "mainnet-beta");

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual(identityName, request.Params.Identity.Name,
                "Identity.Name must match the supplied identityName");
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithCorrectCluster()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");
            const string cluster = "devnet";

            // Act
            _ = _client.Authorize(identityUri, null, "TestApp", cluster);

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual(cluster, request.Params.Cluster,
                "Params.Cluster must match the supplied cluster string");
        }

        [Test]
        public void Authorize_MessageIds_AreIncrementing()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");

            // Act fire two requests
            _ = _client.Authorize(identityUri, null, "TestApp", "mainnet-beta");
            var firstJson = Encoding.UTF8.GetString(_sender.SentMessages[0]);
            var firstRequest = JsonConvert.DeserializeObject<JsonRequest>(firstJson);

            _ = _client.Authorize(identityUri, null, "TestApp", "mainnet-beta");
            var secondJson = Encoding.UTF8.GetString(_sender.SentMessages[1]);
            var secondRequest = JsonConvert.DeserializeObject<JsonRequest>(secondJson);

            // Assert
            Assert.AreEqual(firstRequest.Id + 1, secondRequest.Id,
                "Each successive request must have an Id one greater than the previous");
        }

       
        // Authorize validation
        [Test]
        public void Authorize_ThrowsArgumentException_WhenIdentityUri_IsRelative()
        {
            // Relative identity URIs are not allowed by the MWA spec.
            var relativeUri = new Uri("/relative/path", UriKind.Relative);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _client.Authorize(relativeUri, null, "TestApp", "mainnet-beta"),
                "Authorize must throw ArgumentException when identityUri is relative");
        }

        [Test]
        public void Authorize_DoesNotThrow_WhenIdentityUri_IsNull()
        {
            // Null is allowed here, so this should stay a valid call.
            Assert.DoesNotThrow(() =>
                _client.Authorize(null, null, "TestApp", "mainnet-beta"));
        }

        [Test]
        public void Authorize_ThrowsArgumentException_WhenIconUri_IsAbsolute()
        {
            // iconUri is expected to be relative.
            var absoluteIcon = new Uri("https://example.com/icon.png");

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _client.Authorize(new Uri("https://example.com"), absoluteIcon, "TestApp", "mainnet-beta"),
                "Authorize must throw ArgumentException when iconUri is absolute");
        }

       
        // Reauthorize
        [Test]
        public void Reauthorize_SendsJsonRpc_WithCorrectMethod()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");
            const string authToken = "test-auth-token-abc123";

            // Act
            _ = _client.Reauthorize(identityUri, null, "TestApp", authToken);

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual("reauthorize", request.Method,
                "Method must be 'reauthorize'");
        }

        [Test]
        public void Reauthorize_SendsJsonRpc_WithAuthToken()
        {
            // Arrange
            var identityUri = new Uri("https://example.com");
            const string authToken = "test-auth-token-abc123";

            // Act
            _ = _client.Reauthorize(identityUri, null, "TestApp", authToken);

            // Assert
            var request = DecodeLastRequest();
            Assert.AreEqual(authToken, request.Params.AuthToken,
                "Params.AuthToken must match the supplied auth token");
        }
    }
}
