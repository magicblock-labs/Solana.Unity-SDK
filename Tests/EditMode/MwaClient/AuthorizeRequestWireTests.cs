using System;
using Newtonsoft.Json;
using NUnit.Framework;
using Solana.Unity.SDK;

namespace SolanaMobileStack.Tests.EditMode
{
    public class AuthorizeRequestWireTests
    {
        private static JsonRequest BuildAuthorizeRequest(string chain, string authToken)
        {
            return new JsonRequest
            {
                JsonRpc = "2.0",
                Method = RpcMethodNames.Authorize,
                Params = new JsonRequest.JsonRequestParams
                {
                    Identity = new JsonRequest.JsonRequestIdentity
                    {
                        Uri = new Uri("https://example.com"),
                        Icon = new Uri("/icon.png", UriKind.Relative),
                        Name = "test"
                    },
                    Chain = chain,
                    AuthToken = authToken
                },
                Id = 1
            };
        }

        [Test]
        public void AuthorizeAsync_FreshEmitsChain_NoCluster()
        {
            var req = BuildAuthorizeRequest(chain: "solana:mainnet", authToken: null);
            string json = JsonConvert.SerializeObject(req);

            StringAssert.Contains("\"chain\":\"solana:mainnet\"", json,
                "fresh authorize request must emit chain in CAIP-2 form");
            StringAssert.DoesNotContain("\"cluster\"", json,
                "authorize request must NOT emit v1 cluster key");
            StringAssert.DoesNotContain("\"auth_token\"", json,
                "fresh authorize (authToken == null) must elide auth_token via NullValueHandling.Ignore");
        }

        [Test]
        public void AuthorizeAsync_ReconnectEmitsAuthToken()
        {
            var req = BuildAuthorizeRequest(chain: "solana:devnet", authToken: "cached-token-123");
            string json = JsonConvert.SerializeObject(req);

            StringAssert.Contains("\"chain\":\"solana:devnet\"", json);
            StringAssert.Contains("\"auth_token\":\"cached-token-123\"", json,
                "reconnect path (authToken != null) must emit auth_token");
            StringAssert.DoesNotContain("\"cluster\"", json);
        }
    }
}
