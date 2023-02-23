using System;
using System.Threading.Tasks;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    [RequireComponent(typeof(MainThreadDispatcher))]
    public class Web3 : MonoBehaviour
    {
        [SerializeField]
        private RpcCluster rpcCluster = RpcCluster.DevNet;
        [HideIfEnumValue("rpcCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string customRpc;
        public bool autoConnectOnStartup;
        public string webSocketsRpc;

        public Web3AuthWalletOptions web3AuthWalletOptions;
        
        public PhantomWalletOptions phantomWalletOptions;
        
        private const string StorageMethodStateKey = "StorageMethodKey";

        public WalletBase Wallet;

        public static Web3 Instance;
        
        // Convenience shortnames for accessing commonly used wallet methods
        public static IRpcClient Rpc => Instance != null ? Instance.Wallet?.ActiveRpcClient : null;
        public static IStreamingRpcClient WsRpc => Instance != null ? Instance.Wallet?.ActiveStreamingRpcClient : null;
        public static Account Account => Instance != null ? Instance.Wallet?.Account : null;
        public static WalletBase Base => Instance != null ? Instance.Wallet : null;

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<Account> LoginInGameWallet(string password)
        {
            var inGameWallet = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await inGameWallet.Login(password);
            if (acc != null)
                Wallet = inGameWallet;
            return acc;
        }
        
        public async Task<Account> CreateAccount(string mnemonic, string password)
        {
            Wallet = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            return await Wallet.CreateAccount( mnemonic, password);
        }
        
        public async Task<Account> LoginInWeb3Auth(Provider provider)
        {
            var web3AuthWallet = new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await web3AuthWallet.LoginWithProvider(provider);
            if (acc != null)
                Wallet = web3AuthWallet;
            return acc;
        }
        
        public async Task<Account> LoginPhantom()
        {
            var phantomWallet = new PhantomWallet(phantomWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await phantomWallet.Login();
            if (acc != null)
                Wallet = phantomWallet;
            return acc;
        }

        public async Task<Account> LoginXNFT()
        {
            var XNFTWallet = new XNFTWallet(rpcCluster, customRpc, webSocketsRpc, false);
            var acc = await XNFTWallet.Login();
            if (acc != null)
                Wallet = XNFTWallet;
            return acc;
        }

        public void RpcNodeDropdownSelected(int value)
        {
            rpcCluster = RpcCluster.Custom;
            customRpc = value switch
            {
                (int) RpcCluster.MainNet => "https://red-boldest-uranium.solana-mainnet.quiknode.pro/190d71a30ba3170f66df5e49c8c88870737cd5ce/",
                _ => "https://api.devnet.solana.com"
            };
        }
        
        public void Logout()
        {
            Wallet.Logout();
            Wallet = null;
        }

        #region Data Functions

        private static void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            #if UNITY_WEBGL
            PlayerPrefs.Save();
            #endif
        }

        private static string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        #endregion
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Setup()
        {
            MainThreadUtil.Setup();
        }
    }
    
    /// <summary>
    /// Keeps WalletH for compatibility with older versions of the SDK
    /// </summary>
    [Obsolete("Deprecated, use Web3 instead", true)]
    public static class WalletH
    {
        public static Web3 Instance => Web3.Instance;
        public static IRpcClient Rpc => Instance != null ? Instance.Wallet?.ActiveRpcClient : null;
        public static IStreamingRpcClient WsRpc => Instance != null ? Instance.Wallet?.ActiveStreamingRpcClient : null;
        public static Account Account => Instance != null ? Instance.Wallet?.Account : null;
        public static WalletBase Base => Instance != null ? Instance.Wallet : null;
    }
    
    
    /// <summary>
    /// Keeps SimpleWallet for compatibility with older versions of the SDK
    /// </summary>
    [Obsolete("Deprecated, use Web3 instead", true)]
    public static class SimpleWallet
    {
        public static Web3 Instance => Web3.Instance;
    }
}
