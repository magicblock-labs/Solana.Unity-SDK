using System;
using UnityEngine.Scripting;

[Serializable]
[Preserve]
public class UserInfo
{
    [Preserve]
    public string email { get; set; }
    [Preserve]
    public string name { get; set; }
    [Preserve]
    public string profileImage { get; set; }
    [Preserve]
    public string aggregateVerifier { get; set; }
    [Preserve]
    public string verifier { get; set; }
    [Preserve]
    public string verifierId { get; set; }
    [Preserve]
    public string typeOfLogin { get; set; }
    [Preserve]
    public string dappShare { get; set; }
    [Preserve]
    public string idToken { get; set; }
    [Preserve]
    public string oAuthIdToken { get; set; }
    [Preserve]
    public string oAuthAccessToken { get; set; }
    [Preserve]
    public bool isMfaEnabled { get; set; }
}
