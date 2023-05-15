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
    /// <summary>
    /// Web3 is the main entry point for the Solana Unity SDK.
    /// It is a singleton that manages the connection to the Solana blockchain, allow to login, sign transactions,
    /// listen to events and more.
    /// </summary>
    [RequireComponent(typeof(MainThreadDispatcher))]
    public class Web3 : MonoBehaviour
    {
        #region Variables
        
        public static Web3 Instance;
        
        [Header("Rpc Settings")]

        public RpcCluster rpcCluster = RpcCluster.DevNet;
        public string customRpc = string.Empty;
        public string webSocketsRpc;
        public bool autoConnectOnStartup;
        public WalletBase WalletBase {
        
            get => _wallet;
            set { 
                if(_wallet == null && value?.Account != null) OnLogin?.Invoke(value.Account);
                if(_wallet != null && value == null) OnLogout?.Invoke();
                _wallet = value;
                OnWalletChangeStateInternal?.Invoke();
                SubscribeToWalletEvents().Forget();
                UpdateBalance().Forget();
            }
        }
        
        private static WalletBase _wallet;
        private Web3AuthWallet _web3AuthWallet;
        
        #endregion

        #region Wallet Options

        [Header("Wallets Options")]
        
        public Web3AuthWalletOptions web3AuthWalletOptions;
        public SolanaWalletAdapterOptions solanaWalletAdapterOptions;
        
        #endregion

        #region Events

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
        
        public static Action<Account> OnLogin;
        public static Action OnLogout;

        private static double _solAmount = 0;
        public delegate void BalanceChange(double sol);
        private static event BalanceChange OnBalanceChangeInternal;
        public static event BalanceChange OnBalanceChange
        {
            add
            {
                OnBalanceChangeInternal += value;
                if(Wallet == null) return;
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

        #endregion

        #region Convenience shortnames for accessing commonly used wallet methods
        public static IRpcClient Rpc => Instance != null ? Instance.WalletBase?.ActiveRpcClient : null;
        public static IStreamingRpcClient WsRpc => Instance != null ? Instance.WalletBase?.ActiveStreamingRpcClient : null;
        public static Account Account => Instance != null ? Instance.WalletBase?.Account : null;
        public static WalletBase Wallet => Instance != null ? Instance.WalletBase : null;
        
        [Obsolete("Deprecated, use Wallet instead", false)]
        public static WalletBase Base => Instance != null ? Instance.WalletBase : null;
        
        #endregion

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
                // Try to login if Web3auth session is detected
                _web3AuthWallet ??= new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc);
                _web3AuthWallet.OnLoginNotify += (w) =>
                { 
                    if(w == null) return;
                    WalletBase = _web3AuthWallet;
                };
            }
            catch (Exception e)
            {
                Debug.Log("We3Auth session not detected, " +  e.Message);
            }
            
            #if UNITY_WEBGL
            LoginXNFT().AsUniTask().Forget();
            #endif

            
        }

        /// <summary>
        /// Login to the InGameWallet
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<Account> LoginInGameWallet(string password)
        {
            var inGameWallet = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await inGameWallet.Login(password);
            if (acc != null)
                WalletBase = inGameWallet;
            return acc;
        }
        
        /// <summary>
        /// Create a new InGameWallet
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<Account> CreateAccount(string mnemonic, string password)
        {
            var wallet = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var account = await wallet.CreateAccount( mnemonic, password);
            WalletBase = wallet;
            return account;
        }
        
        /// <summary>
        /// Login to the InGameWallet
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public async Task<Account> LoginWeb3Auth(Provider provider)
        {
            _web3AuthWallet ??=
                new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await _web3AuthWallet.LoginWithProvider(provider);
            if (acc != null)
                WalletBase = _web3AuthWallet;
            return acc;
        }
        
        public async Task<Account> LoginXNFT()
        {
            var isXnft = await SolanaWalletAdapterWebGL.IsXnft();
            if (!isXnft) return null;
            Debug.Log("xNFT detected");
            return await LoginWalletAdapter();
        }

        /// <summary>
        /// Login using the solana wallet adapter
        /// </summary>
        /// <returns></returns>
        public async Task<Account> LoginWalletAdapter()
        {

            if(solanaWalletAdapterOptions.solanaWalletAdapterWebGLOptions.walletAdapterUIPrefab == null)
                solanaWalletAdapterOptions.solanaWalletAdapterWebGLOptions.walletAdapterUIPrefab = Resources.Load<GameObject>("SolanaUnitySDK/WalletAdapterUI");
            if (solanaWalletAdapterOptions.solanaWalletAdapterWebGLOptions.walletAdapterButtonPrefab == null)
                solanaWalletAdapterOptions.solanaWalletAdapterWebGLOptions.walletAdapterButtonPrefab = Resources.Load<GameObject>("SolanaUnitySDK/WalletAdapterButton");
            var walletAdapter = new SolanaWalletAdapter(solanaWalletAdapterOptions, rpcCluster, customRpc, webSocketsRpc, false);
            var acc = await walletAdapter.Login();
            if (acc != null)
                WalletBase = walletAdapter;
            return acc;
        }

        #region Clickable Methods

        public void LoginWithWalletAdapter()
        {
            LoginWalletAdapter().AsUniTask().Forget();
        }

        public void LoginWithWeb3Auth(string provider)
        {
            var parsed = Enum.TryParse<Provider>(provider, out var providerEnum);
            if(!parsed)
                throw new Exception($"Invalid provider, {provider}");
            LoginWeb3Auth(providerEnum).AsUniTask().Forget();
        }


        #endregion

        
        /// <summary>
        /// Wallet logout
        /// </summary>
        public void Logout()
        {
            WalletBase?.Logout();
            WalletBase = null;
            _solAmount = 0;
            _nfts.Clear();
        }
        
        #region Helpers

        /// <summary>
        /// Get the blockhash
        /// </summary>
        /// <param name="commitment"></param>
        /// <param name="useCache"></param>
        /// <param name="maxSeconds">A given blockhash can only be used by transactions for about 60 to 90 seconds
        /// https://docs.solana.com/developing/transaction_confirmation#how-does-transaction-expiration-work</param>
        /// <returns></returns>
        public static Task<string> BlockHash(
            Commitment commitment = Commitment.Finalized,
            bool useCache = true,
            int maxSeconds = 15) =>
            Instance != null ? Instance.WalletBase.GetBlockHash(commitment, useCache, maxSeconds) : null;

        
        
        /// <summary>
        /// Update the solana balance of the current wallet
        /// Notify all registered listeners
        /// </summary>
        /// <param name="commitment"></param>
        public static async UniTask UpdateBalance(Commitment commitment = Commitment.Confirmed)
        {
            if (Instance == null || Instance.WalletBase == null)
                return;
            var balance = await Instance.WalletBase.GetBalance(commitment);
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
            if(Wallet == null) return;
            var tokens = (await Wallet.GetTokenAccounts(commitment))?
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
            await Wallet.AwaitWsRpcConnection();
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
}
