using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Solana.Unity.SolanaMobileStack
{
    [Preserve]
    public sealed class SignInPayload
    {
        [JsonProperty("domain", NullValueHandling = NullValueHandling.Ignore)]
        public string Domain { get; set; }

        [JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }

        [JsonProperty("statement", NullValueHandling = NullValueHandling.Ignore)]
        public string Statement { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        [JsonProperty("chainId", NullValueHandling = NullValueHandling.Ignore)]
        public string ChainId { get; set; }

        [JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
        public string Nonce { get; set; }

        [JsonProperty("issuedAt", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuedAt { get; set; }

        [JsonProperty("expirationTime", NullValueHandling = NullValueHandling.Ignore)]
        public string ExpirationTime { get; set; }

        [JsonProperty("notBefore", NullValueHandling = NullValueHandling.Ignore)]
        public string NotBefore { get; set; }

        [JsonProperty("requestId", NullValueHandling = NullValueHandling.Ignore)]
        public string RequestId { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Resources { get; set; }
    }
}
