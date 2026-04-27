using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public class SignInResult
{
    [JsonProperty("address")]
    [RequiredMember]
    public string Address { get; set; }

    [JsonProperty("signed_message")]
    [RequiredMember]
    public string SignedMessage { get; set; }

    [JsonProperty("signature")]
    [RequiredMember]
    public string Signature { get; set; }

    [JsonProperty("signature_type")]
    [RequiredMember]
    public string SignatureType { get; set; }
}
