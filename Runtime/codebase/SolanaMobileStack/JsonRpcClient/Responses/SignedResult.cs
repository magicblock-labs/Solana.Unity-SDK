using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SolanaMobileStack.JsonRpcClient.Responses
{
    public class SignedResult
    {
        [JsonProperty("signed_payloads")]
        public List<string> SignedPayloads { get; set; }
        
        public List<byte[]> SignedPayloadsBytes => SignedPayloads is { Count: > 0 } ?
            SignedPayloads.Select(Convert.FromBase64String) as List<byte[]> : null;
    }
}