using System;
using System.Collections.Generic;
#nullable enable

public class Web3AuthOptions {
    public string clientId { get; set; }
    public Web3Auth.Network network { get; set; }

    public Web3Auth.BuildEnv buildEnv { get; set; } = Web3Auth.BuildEnv.PRODUCTION;
    public Uri redirectUrl { get; set; }
    public string sdkUrl {
        get {
            if (buildEnv == Web3Auth.BuildEnv.STAGING)
                return "https://staging-auth.web3auth.io/v8";
            else if (buildEnv == Web3Auth.BuildEnv.TESTING)
                return "https://develop-auth.web3auth.io";
            else 
                return "https://auth.web3auth.io/v8";
        }
        set { }
    }

    public string walletSdkUrl {
         get {
            if (buildEnv == Web3Auth.BuildEnv.STAGING)
                return "https://staging-wallet.web3auth.io/v2";
            else if (buildEnv == Web3Auth.BuildEnv.TESTING)
                return "https://develop-wallet.web3auth.io";
            else
                return "https://wallet.web3auth.io/v2";
         }
         set { }
    }
    public WhiteLabelData? whiteLabel { get; set; }
    public Dictionary<string, LoginConfigItem>? loginConfig { get; set; }
    public bool? useCoreKitKey { get; set; } = false;
    public Web3Auth.ChainNamespace? chainNamespace { get; set; } = Web3Auth.ChainNamespace.EIP155;
    public MfaSettings? mfaSettings { get; set; } = null;
    public int sessionTime { get; set; } = 86400;
    public ChainConfig? chainConfig { get; set; }
}