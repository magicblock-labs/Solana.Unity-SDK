using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public class SignAndSendResult
{
    [JsonProperty("signatures")]
    [RequiredMember]
    public List<string> Signatures { get; set; }

    [RequiredMember]
    public List<byte[]> SignaturesBytes => Signatures is { Count: > 0 }
        ? Signatures.Select(Convert.FromBase64String).ToList()
        : new List<byte[]>();
}
