using System;

public class LoginParams {
    public Provider loginProvider { get; set; }
    public bool? relogin { get; set; }
    public string dappShare { get; set; }
    public ExtraLoginOptions extraLoginOptions { get; set; }
    public Uri redirectUrl { get; set; }
    public string appState { get; set; }
    public MFALevel mfaLevel { get; set; }
}