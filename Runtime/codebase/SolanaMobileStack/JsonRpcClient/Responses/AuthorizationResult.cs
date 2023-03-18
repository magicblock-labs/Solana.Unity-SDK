using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SolanaMobileStack.JsonRpcClient.Responses
{
    public class AuthorizationResult {
        
        public class AuthorizationResultAccounts
        {
            [JsonProperty("address")]
            public string Address { get; set; }
            
            [JsonProperty("label")]
            public string Label { get; set; }
        }
        
        [JsonProperty("auth_token")]
        public string AuthToken { get; set; }
        
        [JsonProperty("wallet_uri_base")]
        public Uri WalletUriBase { get; set; }
        
        [JsonProperty("accounts")]
        public List<AuthorizationResultAccounts> Accounts { get; set; }

        public byte[] PublicKey => Accounts is { Count: > 0 } ? Convert.FromBase64String(Accounts[0].Address) : null;

        public string AccountLabel => Accounts is { Count: > 0 } ? Accounts[0].Label : string.Empty;
    }
}