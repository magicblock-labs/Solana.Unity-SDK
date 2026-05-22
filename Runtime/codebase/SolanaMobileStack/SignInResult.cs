using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Solana.Unity.SolanaMobileStack
{
    [Preserve]
    public sealed class SignInResult
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("signed_message")]
        public string SignedMessage { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("signature_type")]
        public string SignatureType { get; set; }
    }
}
