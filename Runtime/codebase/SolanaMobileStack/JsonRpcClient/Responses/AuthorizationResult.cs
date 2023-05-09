using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public class AuthorizationResult {

    public class AuthorizationResultAccounts
    {
        [JsonProperty("address")]
        [RequiredMember]
        public string Address { get; set; }
        
        [JsonProperty("label")]
        [RequiredMember]
        public string Label { get; set; }
    }
    

    [JsonProperty("auth_token")]
    [RequiredMember]
    public string AuthToken { get; set; }
    
    [JsonProperty("wallet_uri_base")]
    [RequiredMember]
    public Uri WalletUriBase { get; set; }
    
    [JsonProperty("accounts")]
    [RequiredMember]
    public List<AuthorizationResultAccounts> Accounts { get; set; }
    
    [RequiredMember]
    public byte[] PublicKey => Accounts is { Count: > 0 } ? Convert.FromBase64String(Accounts[0].Address) : null;

    [RequiredMember]
    public string AccountLabel => Accounts is { Count: > 0 } ? Accounts[0].Label : string.Empty;
}