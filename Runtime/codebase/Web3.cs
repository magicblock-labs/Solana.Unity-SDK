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
        public string customRpc = string.Empty;
        public bool autoConnectOnStartup;
        public string webSocketsRpc;
        
        #region Wallet Options

        public Web3AuthWalletOptions web3AuthWalletOptions;
        
        public PhantomWalletOptions phantomWalletOptions;
        
        public SolanaMobileWalletAdapterOptions solanaMobileWalletOptions;
        
        #endregion
        
        public delegate void WalletChange();
        private static event WalletChange OnWalletChangeStateInternal;
        public static event WalletChange OnWalletChangeState
        {
            add
            {
                OnWalletChangeStateInternal += value;
                OnWalletChangeStateInternal?.Invoke();
            }
            remove => OnWalletChangeStateInternal -= value;
        }

        private static WalletBase _wallet;
        public WalletBase Wallet {
        
            get => _wallet;
            private set { 
                _wallet = value;
                OnWalletChangeStateInternal?.Invoke();
            }
        }

        public static Web3 Instance;

        #region Convenience shortnames for accessing commonly used wallet methods
        public static IRpcClient Rpc => Instance != null ? Instance.Wallet?.ActiveRpcClient : null;
        public static IStreamingRpcClient WsRpc => Instance != null ? Instance.Wallet?.ActiveStreamingRpcClient : null;
        public static Account Account => Instance != null ? Instance.Wallet?.Account : null;
        public static WalletBase Base => Instance != null ? Instance.Wallet : null;
        
        #endregion
        
        private Web3AuthWallet _web3AuthWallet;


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

        private void Start()
        {
            try
            {
                RpcNodeDropdownSelected(PlayerPrefs.GetInt("rpcCluster", 0));
                _web3AuthWallet ??= new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc);
                _web3AuthWallet.OnLoginNotify += (w) =>
                { 
                    if(w == null) return;
                    Wallet = _web3AuthWallet;
                    Debug.Log("Wallet set to Web3Auth, " + Wallet.GetType().FullName);
                };
            }
            catch (Exception e)
            {
                Debug.Log("We3Auth session not detected, " +  e.Message);
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
            _web3AuthWallet ??=
                new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await _web3AuthWallet.LoginWithProvider(provider);
            if (acc != null)
                Wallet = _web3AuthWallet;
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
        
        public async Task<Account> LoginSolanaMobileStack()
        {
            var solanaWallet = new SolanaMobileWalletAdapter(solanaMobileWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await solanaWallet.Login();
            if (acc != null)
                Wallet = solanaWallet;
            return acc;
        }

        public async Task<Account> LoginXNFT()
        {
            var xnftWallet = new XNFTWallet(rpcCluster, customRpc, webSocketsRpc, false);
            var acc = await xnftWallet.Login();
            if (acc != null)
                Wallet = xnftWallet;
            return acc;
        }

        public void RpcNodeDropdownSelected(int value)
        {
            PlayerPrefs.SetInt("rpcCluster", value);
            PlayerPrefs.Save();
            rpcCluster = (RpcCluster) value;
            customRpc = value switch
            {
                (int) RpcCluster.MainNet => "https://red-boldest-uranium.solana-mainnet.quiknode.pro/190d71a30ba3170f66df5e49c8c88870737cd5ce/",
                (int) RpcCluster.TestNet => "https://api.testnet.solana.com",
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
    /// Keeps SimpleWallet for compatibility with older versions of the SDK
    /// </summary>
    [Obsolete("Deprecated, use Web3 instead", true)]
    public static class SimpleWallet
    {
        public static Web3 Instance => Web3.Instance;
    }
}
