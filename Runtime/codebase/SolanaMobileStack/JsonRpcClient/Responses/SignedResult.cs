using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public class SignedResult
{
    [JsonProperty("signed_payloads")]
    [RequiredMember]
    public List<string> SignedPayloads { get; set; }
     
    [RequiredMember]
    public List<byte[]> SignedPayloadsBytes => SignedPayloads is { Count: > 0 } ?
        SignedPayloads.Select(Convert.FromBase64String).ToList() : null;
}