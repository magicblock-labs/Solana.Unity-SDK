using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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

            [JsonProperty("cluster", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
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

            [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public SignAndSendOptions Options { get; set; }

            [RequiredMember]
            public JsonRequestParams()
            {
            }
        }

        [Serializable]
        [Preserve]
        public class SignAndSendOptions
        {
            [JsonProperty("min_context_slot", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public ulong? MinContextSlot { get; set; }

            [JsonProperty("commitment", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public string Commitment { get; set; }

            [JsonProperty("skip_preflight", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public bool? SkipPreflight { get; set; }

            [JsonProperty("max_retries", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public int? MaxRetries { get; set; }

            [JsonProperty("wait_for_commitment_to_send_next_transaction", NullValueHandling = NullValueHandling.Ignore)]
            [RequiredMember]
            public bool? WaitForCommitmentToSendNextTransaction { get; set; }

            [Preserve]
            [RequiredMember]
            public SignAndSendOptions()
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