using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SolanaMobileStack.JsonRpcClient
{
    public class JsonRequest
    {
        public class JsonRequestIdentity
        {
            [JsonProperty("uri", NullValueHandling=NullValueHandling.Ignore)]
            public Uri Uri { get; set; }
        
            [JsonProperty("icon", NullValueHandling=NullValueHandling.Ignore)]
            public Uri Icon { get; set; }
        
            [JsonProperty("name", NullValueHandling=NullValueHandling.Ignore)]
            public string Name { get; set; }
        }
            
        public class JsonRequestParams
        {
            [JsonProperty("identity", NullValueHandling=NullValueHandling.Ignore)]
            public JsonRequestIdentity Identity { get; set; }
        
            [JsonProperty("cluster", NullValueHandling=NullValueHandling.Ignore)]
            public string Cluster { get; set; }

            [JsonProperty("auth_token", NullValueHandling=NullValueHandling.Ignore)]
            public string AuthToken { get; set; }
                
            [JsonProperty("payloads", NullValueHandling=NullValueHandling.Ignore)]
            public List<string> Payloads { get; set; }
                
            [JsonProperty("addresses", NullValueHandling=NullValueHandling.Ignore)]
            public List<string> Addresses { get; set; }
        }
            
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }
        
        [JsonProperty("method")]
        public string Method { get; set; }
        
        [JsonProperty("params")]
        public JsonRequestParams Params { get; set; }
        
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}