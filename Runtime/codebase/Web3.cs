using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    [RequireComponent(typeof(MainThreadDispatcher))]
    public class Web3 : MonoBehaviour
    {
        public RpcCluster rpcCluster = RpcCluster.DevNet;
        public string customRpc = string.Empty;
        public bool autoConnectOnStartup;
        public string webSocketsRpc;
        
        #region Wallet Options

        public Web3AuthWalletOptions web3AuthWalletOptions;
        
        public PhantomWalletOptions phantomWalletOptions;
        
        public SolanaMobileWalletAdapterOptions solanaMobileWalletOptions;
        
        public SolanaWalletAdapterWebGLOptions solanaWalletAdapterWebGLOptions;
        
        #endregion

        public delegate void WalletInstance();
        public static event WalletInstance OnWalletInstance;
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
        
        private static double _solAmount = 0;
        public delegate void BalanceChange(double sol);
        private static event BalanceChange OnBalanceChangeInternal;
        public static event BalanceChange OnBalanceChange
        {
            add
            {
                OnBalanceChangeInternal += value;
                OnBalanceChangeInternal?.Invoke(_solAmount);
                UpdateBalance().Forget();
            }
            remove => OnBalanceChangeInternal -= value;
        }
        
        private static List<Nft.Nft> _nfts = new();
        public delegate void NFTsUpdate(List<Nft.Nft> nft);
        private static event NFTsUpdate OnNFTsUpdateInternal;
        public static event NFTsUpdate OnNFTsUpdate
        {
            add
            {
                OnNFTsUpdateInternal += value;
                OnNFTsUpdateInternal?.Invoke(_nfts);
                UpdateNFTs().Forget();
            }
            remove => OnNFTsUpdateInternal -= value;
        }

        private static WalletBase _wallet;
        public WalletBase Wallet {
        
            get => _wallet;
            private set { 
                _wallet = value;
                OnWalletChangeStateInternal?.Invoke();
                SubscribeToWalletEvents().Forget();
                UpdateBalance().Forget();
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
                OnWalletInstance?.Invoke();
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
        
        public async Task<Account> LoginWalletAdapter()
        {

            if(solanaWalletAdapterWebGLOptions.walletAdapterUIPrefab == null)
                solanaWalletAdapterWebGLOptions.walletAdapterUIPrefab = Resources.Load<GameObject>("SolanaUnitySDK/WalletAdapterUI");
            if (solanaWalletAdapterWebGLOptions.walletAdapterButtonPrefab == null)
                solanaWalletAdapterWebGLOptions.walletAdapterButtonPrefab = Resources.Load<GameObject>("SolanaUnitySDK/WalletAdapterButton");
            var walletAdapter = new SolanaWalletAdapterWebGL(solanaWalletAdapterWebGLOptions, rpcCluster, customRpc, webSocketsRpc, false);
            var acc = await walletAdapter.Login();
            if (acc != null)
                Wallet = walletAdapter;
            return acc;
        }


        
        public void Logout()
        {
            Wallet?.Logout();
            Wallet = null;
            _solAmount = 0;
            _nfts.Clear();
        }
        
        #region Helpers
        
        
        /// <summary>
        /// Update the solana balance of the current wallet
        /// Notify all registered listeners
        /// </summary>
        /// <param name="commitment"></param>
        public static async UniTask UpdateBalance(Commitment commitment = Commitment.Confirmed)
        {
            if (Instance == null || Instance.Wallet == null)
                return;
            var balance = await Instance.Wallet.GetBalance(commitment);
            _solAmount = balance;
            OnBalanceChangeInternal?.Invoke(balance);
        }
        
        /// <summary>
        /// Update the list of NFTs owned by the current wallet
        /// Notify all registered listeners
        /// </summary>
        /// <param name="commitment"></param>
        public static async UniTask UpdateNFTs(Commitment commitment = Commitment.Confirmed)
        {
            if(Base == null) return;
            var tokens = (await Base.GetTokenAccounts(commitment))?
                .ToList()
                .FindAll(m => m.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 1);
            if(tokens == null) return;
            
            // Remove tokens not owned anymore
            var tkToRemove = new List<Nft.Nft>();
            _nfts.ForEach(tk =>
            {
                var match = tokens.Where(t =>
                    t?.Account?.Data?.Parsed?.Info?.Mint == tk?.metaplexData?.data?.mint).ToArray();
                if (match.Length == 0 || match.Any(m => m.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 0))
                {
                    tkToRemove.Add(tk);
                }
            });
            tkToRemove.ForEach(tk => _nfts.Remove(tk));
            
            // Remove duplicated nfts
            _nfts = _nfts
                .GroupBy(x => x.metaplexData.data.mint)
                .Select(x => x.First())
                .ToList()
                .FindAll(x => x.metaplexData.data.offchainData != null);

            // Fetch nfts
            if (tokens is {Count: > 0})
            {
                var toFetch = tokens
                    .Where(item => item.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 1)
                    .Where(item => _nfts
                        .All(t => t.metaplexData.data.mint!= item.Account.Data.Parsed.Info.Mint));
                foreach (var item in toFetch)
                {
                    Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, Rpc).AsUniTask()
                        .ContinueWith(nft =>
                        {
                            _nfts.Add(nft);
                            OnNFTsUpdateInternal?.Invoke(_nfts);
                        }).Forget();
                }
            }
        }
        
        private static async UniTask SubscribeToWalletEvents(Commitment commitment = Commitment.Confirmed)
        {
            if(WsRpc == null) return;
            await Base.AwaitWsRpcConnection();
            await WsRpc.SubscribeAccountInfoAsync(
                Account.PublicKey,
                (_, accountInfo) =>
                {
                    Debug.Log("Account changed!, updated lamport: " + accountInfo.Value.Lamports);
                    _solAmount = accountInfo.Value.Lamports / 1000000000d;
                    OnBalanceChangeInternal?.Invoke(_solAmount);
                    UpdateNFTs(commitment).Forget();
                },
                commitment
            );
        }
        
        #endregion

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
