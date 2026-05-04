using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Solana.Unity.SDK.Tests.EditMode.Mocks;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.MwaClient
{
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
            var identityUri = new Uri("https://example.com");
            var iconUri = new Uri("/icon.png", UriKind.Relative);

            _ = _client.AuthorizeAsync(identityUri, iconUri, "TestApp", "solana:mainnet", null, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.AreEqual("authorize", request.Method);
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithVersion2_0()
        {
            var identityUri = new Uri("https://example.com");

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:mainnet", null, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.AreEqual("2.0", request.JsonRpc);
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithNonZeroId()
        {
            var identityUri = new Uri("https://example.com");

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:mainnet", null, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.Greater(request.Id, 0);
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithIdentityName()
        {
            var identityUri = new Uri("https://example.com");

            _ = _client.AuthorizeAsync(identityUri, null, "CrossyRoad", "solana:mainnet", null, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.AreEqual("CrossyRoad", request.Params.Identity.Name);
        }

        [Test]
        public void Authorize_SendsJsonRpc_WithCorrectChain()
        {
            var identityUri = new Uri("https://example.com");

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:devnet", null, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.AreEqual("solana:devnet", request.Params.Chain);
        }

        [Test]
        public void Authorize_MessageIds_AreIncrementing()
        {
            var identityUri = new Uri("https://example.com");

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:mainnet", null, CancellationToken.None);
            var firstJson = Encoding.UTF8.GetString(_sender.SentMessages[0]);
            var firstRequest = JsonConvert.DeserializeObject<JsonRequest>(firstJson);

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:mainnet", null, CancellationToken.None);
            var secondJson = Encoding.UTF8.GetString(_sender.SentMessages[1]);
            var secondRequest = JsonConvert.DeserializeObject<JsonRequest>(secondJson);

            Assert.AreEqual(firstRequest.Id + 1, secondRequest.Id);
        }

        // Authorize validation
        [Test]
        public void Authorize_ThrowsArgumentException_WhenIdentityUri_IsRelative()
        {
            var relativeUri = new Uri("/relative/path", UriKind.Relative);

            Assert.Throws<ArgumentException>(() =>
                _client.AuthorizeAsync(relativeUri, null, "TestApp", "solana:mainnet", null, CancellationToken.None));
        }

        [Test]
        public void Authorize_DoesNotThrow_WhenIdentityUri_IsNull()
        {
            Assert.DoesNotThrow(() =>
                _client.AuthorizeAsync(null, null, "TestApp", "solana:mainnet", null, CancellationToken.None));
        }

        [Test]
        public void Authorize_ThrowsArgumentException_WhenIconUri_IsAbsolute()
        {
            var absoluteIcon = new Uri("https://example.com/icon.png");

            Assert.Throws<ArgumentException>(() =>
                _client.AuthorizeAsync(new Uri("https://example.com"), absoluteIcon, "TestApp", "solana:mainnet", null, CancellationToken.None));
        }

        // Authorize with features and sign_in_payload
        [Test]
        public void Authorize_SendsFeatures_WhenProvided()
        {
            var identityUri = new Uri("https://example.com");
            var features = new[] { "solana:signInWithSolana" };

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:mainnet", null,
                features, null, null, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.IsNotNull(request.Params.Features);
            Assert.AreEqual(1, request.Params.Features.Count);
            Assert.AreEqual("solana:signInWithSolana", request.Params.Features[0]);
        }

        [Test]
        public void Authorize_SendsSignInPayload_WhenProvided()
        {
            var identityUri = new Uri("https://example.com");
            var payload = new Solana.Unity.SolanaMobileStack.SignInPayload
            {
                Domain = "example.com",
                Statement = "Sign in",
            };

            _ = _client.AuthorizeAsync(identityUri, null, "TestApp", "solana:mainnet", null,
                null, null, payload, CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.IsNotNull(request.Params.SignInPayload);
            Assert.AreEqual("example.com", request.Params.SignInPayload.Domain);
            Assert.AreEqual("Sign in", request.Params.SignInPayload.Statement);
        }

        // Clone authorization
        [Test]
        public void CloneAuthorization_SendsCorrectMethod()
        {
            _ = _client.CloneAuthorizationAsync("test-token", CancellationToken.None);

            var request = DecodeLastRequest();
            Assert.AreEqual("clone_authorization", request.Method);
            Assert.AreEqual("test-token", request.Params.AuthToken);
        }
    }
}
