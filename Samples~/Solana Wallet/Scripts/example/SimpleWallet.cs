using System.Diagnostics;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public enum StorageMethod { JSON, SimpleTxt }
    
    [RequireComponent(typeof(MainThreadDispatcher))]
    public class SimpleWallet : MonoBehaviour
    {
        [SerializeField]
        private RpcCluster rpcCluster = RpcCluster.DevNet;
        [HideIfEnumValue("rpcCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string customRpc;
        public bool autoConnectOnStartup;

        public StorageMethod storageMethod;
        
        public Web3AuthWalletOptions web3AuthWalletOptions;
        
        public PhantomWalletOptions phantomWalletOptions;
        
        private const string StorageMethodStateKey = "StorageMethodKey";

        public WalletBase Wallet;

        public static SimpleWallet Instance;

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
        
        public void Start()
        {
            ChangeState(storageMethod.ToString());
            if (PlayerPrefs.HasKey(StorageMethodStateKey))
            {
                var storageMethodString = LoadPlayerPrefs(StorageMethodStateKey);

                if(storageMethodString != storageMethod.ToString())
                {
                    storageMethodString = storageMethod.ToString();
                    ChangeState(storageMethodString);
                }

                if (storageMethodString == StorageMethod.JSON.ToString())
                    StorageMethodReference = StorageMethod.JSON;
                else if (storageMethodString == StorageMethod.SimpleTxt.ToString())
                    StorageMethodReference = StorageMethod.SimpleTxt;
            }
            else
                StorageMethodReference = StorageMethod.SimpleTxt;          
        }

        public async Task<Account> LoginInGameWallet(string password)
        {
            var inGameWallet = new InGameWallet(rpcCluster, customRpc, autoConnectOnStartup);
            var acc = await inGameWallet.Login(password);
            if (acc != null)
                Wallet = inGameWallet;
            return acc;
        }
        
        public async Task<Account> CreateAccount(string mnemonic, string password)
        {
            Wallet = new InGameWallet(rpcCluster, customRpc, autoConnectOnStartup);
            return await Wallet.CreateAccount( mnemonic, password);
        }
        
        public async Task<Account> LoginInWeb3Auth(Provider provider)
        {
            var web3AuthWallet = new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, autoConnectOnStartup);
            var acc = await web3AuthWallet.LoginWithProvider(provider);
            if (acc != null)
                Wallet = web3AuthWallet;
            return acc;
        }
        
        public async Task<Account> LoginPhantom()
        {
            var phantomWallet = new PhantomWallet(phantomWalletOptions, rpcCluster, customRpc, autoConnectOnStartup);
            var acc = await phantomWallet.Login();
            if (acc != null)
                Wallet = phantomWallet;
            return acc;
        }

        public async Task<Account> LoginXNFT()
        {
            var XNFTWallet = new XNFTWallet(rpcCluster, customRpc, autoConnectOnStartup);
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
                (int) RpcCluster.MainNet => "https://rpc.ankr.com/solana",
                _ => "https://rpc.ankr.com/solana_devnet"
            };
        }
        
        public void Logout()
        {
            Wallet.Logout();
            Wallet = null;
        }

        private void ChangeState(string state)
        {
            SavePlayerPrefs(StorageMethodStateKey, storageMethod.ToString());
        }

        public StorageMethod StorageMethodReference
        {
            get => storageMethod;
            private set { storageMethod = value; ChangeState(storageMethod.ToString()); }
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
    }
}
