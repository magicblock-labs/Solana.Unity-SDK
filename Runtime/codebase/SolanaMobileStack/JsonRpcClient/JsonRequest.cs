using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;


// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{

    [Serializable]
    [Preserve]
    public class JsonRequest
    {
        [Serializable]
        [Preserve]
        public class JsonRequestIdentity
        {
            [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public Uri Uri { get; set; }

            [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public Uri Icon { get; set; }

            [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public string Name { get; set; }

            [Preserve]
            [RequiredMember]
            public JsonRequestIdentity()
            {
            }
        }

        [Serializable]
        [Preserve]
        public class JsonRequestParams
        {
            [JsonProperty("identity", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public JsonRequestIdentity Identity { get; set; }

            [JsonProperty("chain", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public string Chain { get; set; }

            [JsonProperty("cluster", NullValueHandling = NullValueHandling.Ignore)]
            public string Cluster { get; set; }

            [JsonProperty("auth_token", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            
            public string AuthToken { get; set; }

            [JsonProperty("payloads", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public List<string> Payloads { get; set; }

            [JsonProperty("addresses", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public List<string> Addresses { get; set; }

            [JsonProperty("features", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public List<string> Features { get; set; }

            [JsonProperty("sign_in_payload", NullValueHandling = NullValueHandling.Ignore)]
            public Solana.Unity.SolanaMobileStack.SignInPayload SignInPayload { get; set; }

            [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
            public JObject Options { get; set; }

            [RequiredMember]
            public JsonRequestParams()
            {
            }
        }

        [JsonProperty("jsonrpc")] 
        [RequiredMember]
        public string JsonRpc { get; set; }

        [JsonProperty("method")] 
        [RequiredMember]
        public string Method { get; set; }

        [JsonProperty("params")] 
        [RequiredMember]
        public JsonRequestParams Params { get; set; }

        [JsonProperty("id")] 
        [RequiredMember]
        public int Id { get; set; }
    }
}