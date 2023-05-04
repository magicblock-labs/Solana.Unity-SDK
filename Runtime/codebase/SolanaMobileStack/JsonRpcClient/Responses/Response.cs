using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{

    [Preserve]
    public class Response<T>
    {
        [Preserve]
        public class ResponseError
        {
            [JsonProperty("code")] 
            [RequiredMember]
            public long Code { get; set; }

            [JsonProperty("message")] 
            [RequiredMember]
            public string Message { get; set; }
        }


        [JsonProperty("jsonrpc")] 
        [RequiredMember]
        public string JsonRpc { get; set; }

        [JsonProperty("result")]
        [RequiredMember]
        
        public T Result { get; set; }

        [JsonProperty("id")] 
        [RequiredMember]
        public long Id { get; set; }

        [JsonProperty("error")] 
        [RequiredMember]
        public ResponseError Error { get; set; }
        
        [RequiredMember]
        public bool WasSuccessful => Error is null;
        
        [RequiredMember]
        public bool Failed => Error is not null;
    }
}