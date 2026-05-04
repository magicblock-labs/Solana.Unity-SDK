using System.Collections.Generic;
using Newtonsoft.Json;
using Solana.Unity.SolanaMobileStack;
using UnityEngine.Scripting;


// ReSharper disable once CheckNamespace
[Preserve]
public sealed class AuthorizationResult
{
    [JsonProperty("auth_token")]
    [RequiredMember]
    public string AuthToken { get; set; };

    [JsonProperty("accounts")]
    [RequiredMember]
    public List<AccountInfo> Accounts { get; set; } = new();

    [JsonProperty("wallet_uri_base")]
    public string WalletUriBase { get; set; }

    [JsonProperty("wallet_icon")]
    public string WalletIcon { get; set; }

    public SignInResult SignInResult { get; set; }
}
