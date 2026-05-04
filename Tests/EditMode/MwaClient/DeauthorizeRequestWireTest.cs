using Newtonsoft.Json;
using NUnit.Framework;
using Solana.Unity.SDK;

namespace SolanaMobileStack.Tests.EditMode
{
    public class DeauthorizeRequestWireTest
    {
        private static JsonRequest BuildDeauthorizeRequest(string authToken)
        {
            return new JsonRequest
            {
                JsonRpc = "2.0",
                Method = "deauthorize",
                Params = new JsonRequest.JsonRequestParams
                {
                    AuthToken = authToken
                },
                Id = 1
            };
        }

        [Test]
        public void DeauthorizeRequest_EmitsMethodAndAuthToken()
        {
            var req = BuildDeauthorizeRequest("test-auth-token-123");
            string json = JsonConvert.SerializeObject(req);

            StringAssert.Contains("\"method\":\"deauthorize\"", json);
            StringAssert.Contains("\"auth_token\":\"test-auth-token-123\"", json);
        }

        [Test]
        public void DeauthorizeRequest_OmitsIdentityAndChain()
        {
            var req = BuildDeauthorizeRequest("tok");
            string json = JsonConvert.SerializeObject(req);

            StringAssert.DoesNotContain("\"identity\"", json,
                "deauthorize must not include identity block");
            StringAssert.DoesNotContain("\"chain\"", json,
                "deauthorize must not include chain field");
            StringAssert.DoesNotContain("\"payloads\"", json,
                "deauthorize must not include payloads");
        }
    }
}
