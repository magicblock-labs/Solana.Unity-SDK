
using System;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public class Web3AuthResponse
{
    public string privKey { get; set; }
    public string ed25519PrivKey { get; set; }
    public UserInfo userInfo { get; set; }
    public string error { get; set; }
    public string sessionId { get; set; }

    [Preserve]
    public Web3AuthResponse()
    {
    }

    [Preserve]
    public Web3AuthResponse(string privKey, string ed25519PrivKey, UserInfo userInfo, string sessionId)
    {
        this.privKey = privKey;
        this.ed25519PrivKey = ed25519PrivKey;
        this.userInfo = userInfo;
        this.sessionId = sessionId;
    }

    [Preserve]
    public Web3AuthResponse(string privKey, string ed25519PrivKey, UserInfo userInfo, string error, string sessionId)
    {
        this.privKey = privKey;
        this.ed25519PrivKey = ed25519PrivKey;
        this.userInfo = userInfo;
        this.error = error;
        this.sessionId = sessionId;
    }
}