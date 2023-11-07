using System;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    [Serializable]
    public class SolflareWalletOptions : PhantomWalletOptions
    {
        [SerializeField]
        private string solflareApiVersion = "v1";
        public override string ApiVersion
        {
            get => solflareApiVersion;
            set => solflareApiVersion = value;
        }

        [SerializeField]
        private string solflareAppMetaDataUrl = "https://github.com/magicblock-labs/Solana.Unity-SDK";
        public override string AppMetaDataUrl
        {
            get => solflareAppMetaDataUrl;
            set => solflareAppMetaDataUrl = value;
        }

        [SerializeField]
        private string solflareDeeplinkUrlScheme = "unitydl";
        public override string DeeplinkUrlScheme
        {
            get => solflareDeeplinkUrlScheme;
            set => solflareDeeplinkUrlScheme = value;
        }

        [SerializeField]
        private string solflareSessionEncryptionPassword = "use a strong password";
        public override string SessionEncryptionPassword
        {
            get => solflareSessionEncryptionPassword;
            set => solflareSessionEncryptionPassword = value;
        }

        [SerializeField]
        private string solflareBaseUrl = "https://solflare.com";
        public override string BaseUrl
        {
            get => solflareBaseUrl;
            set => solflareBaseUrl = value;
        }

        [SerializeField]
        private string solflareWalletName = "solflare";
        public override string WalletName
        {
            get => solflareWalletName;
            set => solflareWalletName = value;
        }
    }
    
    public class SolflareDeepLink: PhantomDeepLink
    {
        public SolflareDeepLink(SolflareWalletOptions deepLinksWalletOptions, 
            RpcCluster rpcCluster = RpcCluster.DevNet, 
            string customRpcUri = null, 
            string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) : base(deepLinksWalletOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
        }
    }
}