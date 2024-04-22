using System;
using UnityEngine.Scripting;

[Serializable]
[Preserve]
public class Web3AuthResponse
{
    [Preserve]
    public string privKey { get; set; }
    [Preserve]
    public string ed25519PrivKey { get; set; }
    [Preserve]
    public UserInfo userInfo { get; set; }
    [Preserve]
    public string error { get; set; }
    [Preserve]
    public string sessionId { get; set; }
    [Preserve]
    public string coreKitKey { get; set; }
    [Preserve]
    public string coreKitEd25519PrivKey { get; set; }
}