using Newtonsoft.Json;

namespace SolanaMobileStack.JsonRpcClient.Responses
{
    public class Response<T>
    {
        public class ResponseError
        {
            [JsonProperty("code")]
            public long Code { get; set; }
    
            [JsonProperty("message")]
            public string Message { get; set; }
        }


        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }
    
        [JsonProperty("result")]
        public T Result { get; set; }
    
        [JsonProperty("id")]
        public long Id { get; set; }
    
        [JsonProperty("error")]
        public ResponseError Error { get; set; }
        public bool WasSuccessful => Error is null;
        
        public bool Failed => Error is not null;
    }
}