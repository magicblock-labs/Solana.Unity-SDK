using System;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public class LoginParams
{
    [Preserve]
    public Provider loginProvider { get; set; }
    [Preserve]
    public string dappShare { get; set; }
    [Preserve]
    public ExtraLoginOptions extraLoginOptions { get; set; }
    [Preserve]
    public Uri redirectUrl { get; set; }
    [Preserve]
    public string appState { get; set; }
    [Preserve]
    public MFALevel mfaLevel { get; set; }
    [Preserve]
    public Curve curve { get; set; } = Curve.SECP256K1;
    [Preserve]
    public string dappUrl { get; set; }
}