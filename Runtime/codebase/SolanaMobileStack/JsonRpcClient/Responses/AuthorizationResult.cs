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
    public string AuthToken { get; set; }

    [JsonProperty("accounts")]
    [RequiredMember]
    public List<AccountInfo> Accounts { get; set; } = new();

    [JsonProperty("wallet_uri_base")]
    public string WalletUriBase { get; set; }

    [JsonProperty("wallet_icon")]
    public string WalletIcon { get; set; }

    public SignInResult SignInResult { get; set; }

    [System.Obsolete("Use AuthorizationHelpers.PrimaryAccountPublicKeyBytes() instead")]
    public byte[] PublicKey => Accounts is { Count: > 0 }
        ? System.Convert.FromBase64String(Accounts[0].Address)
        : null;

    [System.Obsolete("Use AuthorizationHelpers.PrimaryAccount().Label instead")]
    public string AccountLabel => Accounts is { Count: > 0 }
        ? Accounts[0].Label ?? string.Empty
        : string.Empty;
}
