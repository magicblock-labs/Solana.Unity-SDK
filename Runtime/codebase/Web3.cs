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
                var currentWallet = _wallet;
                _wallet = value;
                if (currentWallet == null && value?.Account != null)
                {
                    value.RpcMaxHits = RpcMaxHits;
                    value.RpcMaxHitsPerSeconds = RpcMaxHitsPerSeconds;
                    OnLogin?.Invoke(value.Account);
                    UpdateBalance().Forget();
                    if(OnNFTsUpdateInternal != null && AutoLoadNfts) UpdateNFTs().Forget();
                    SubscribeToWalletEvents().Forget();
                }
                if(currentWallet != null && value == null) OnLogout?.Invoke();
                OnWalletChangeStateInternal?.Invoke();

            }
        }
        
        private static WalletBase _wallet;
        private Web3AuthWallet _web3AuthWallet;
        
        private int _defaultRpcMaxHits = 30;
        public int RpcMaxHits
        {

            get => _wallet?.RpcMaxHits ?? _defaultRpcMaxHits;
            set
            {
                if (_wallet != null) _wallet.RpcMaxHits = value;
                else _defaultRpcMaxHits = value;
            }
        }
        
        private int _defaultRpcMaxHitsPerSeconds = 1;
        public int RpcMaxHitsPerSeconds
        {

            get => _wallet?.RpcMaxHitsPerSeconds ?? _defaultRpcMaxHitsPerSeconds;
            set
            {
                if (_wallet != null) _wallet.RpcMaxHitsPerSeconds = value;
                else _defaultRpcMaxHitsPerSeconds = value;
            }
        }

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
        public static Action OnWebSocketConnect;

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
        
        private static List<Nft.Nft> _nfts;
        private static bool _isLoadingNfts;
        public static int NftLoadingRequestsDelay { get; set; } = 0;

        public delegate void NFTsUpdate(List<Nft.Nft> nfts, int total);
        private static event NFTsUpdate OnNFTsUpdateInternal;
        public static event NFTsUpdate OnNFTsUpdate
        {
            add
            {
                OnNFTsUpdateInternal += value;
                if(Wallet == null) return;
                if(_nfts != null) OnNFTsUpdateInternal?.Invoke(_nfts, _nfts.Count);
                if(AutoLoadNfts) UpdateNFTs().Forget();
            }
            remove => OnNFTsUpdateInternal -= value;
        }
        public static bool? LoadNftsTextureByDefault = null;
        public static bool AutoLoadNfts = true;

        #endregion

        #region Convenience shortnames for accessing commonly used wallet methods
        public static IRpcClient Rpc => Instance != null && Instance.WalletBase != null
            ? Instance.WalletBase?.ActiveRpcClient : Instance != null ? Instance.GetDefaultRpc() : null;

        public static IStreamingRpcClient WsRpc => Instance != null && Instance.WalletBase != null 
            ? Instance.WalletBase?.ActiveStreamingRpcClient : Instance != null ? Instance.GetDefaultWsRpc() : null;
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
                    OnWalletChangeStateInternal?.Invoke();
                    SubscribeToWalletEvents().Forget();
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
        
        public async Task<Account> LoginWeb3Auth(LoginParams loginParams)
        {
            _web3AuthWallet ??=
                new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await _web3AuthWallet.LoginWithParams(loginParams);
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
            Wallet?.Logout();
            WalletBase = null;
            _solAmount = 0;
            if(_nfts != null) _nfts.Clear();
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
            Commitment commitment = Commitment.Confirmed,
            bool useCache = true,
            int maxSeconds = 0) =>
            Instance != null ? Instance.WalletBase.GetBlockHash(commitment, useCache, maxSeconds) : null;

        
        
        /// <summary>
        /// Update the solana balance of the current wallet
        /// Notify all registered listeners
        /// </summary>
        /// <param name="commitment"></param>
        public static async UniTask UpdateBalance(Commitment commitment = Commitment.Processed)
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
        public static async UniTask UpdateNFTs(Commitment commitment = Commitment.Processed)
        {
            if(_isLoadingNfts) return;
            _isLoadingNfts = true;
            await LoadNFTs(notifyRegisteredListeners: true, commitment: commitment);
            _isLoadingNfts = false;
        }

        /// <summary>
        /// Update the list of NFTs owned by the current wallet
        /// Notify all registered listeners
        /// </summary>
        /// <param name="loadTexture"></param>
        /// <param name="notifyRegisteredListeners">If true, notify the register listeners</param>
        /// <param name="requestsMillisecondsDelay">Add a delay between requests</param>
        /// <param name="commitment"></param>
        public static async UniTask<List<Nft.Nft>> LoadNFTs(
            bool loadTexture = true, 
            bool notifyRegisteredListeners = true,
            int requestsMillisecondsDelay = 0,
            Commitment commitment = Commitment.Processed)
        {
            loadTexture = LoadNftsTextureByDefault ?? loadTexture;
            if(Wallet == null) return null;
            var tokens = (await Wallet.GetTokenAccounts(commitment))?
                .ToList()
                .FindAll(m => m.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 1);
            if(tokens == null) return null;
            
            // Remove tokens not owned anymore
            var tkToRemove = new List<Nft.Nft>();
            _nfts ??= new List<Nft.Nft>();
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
            List<UniTask> loadingTasks = new List<UniTask>();
            List<Nft.Nft> nfts = new List<Nft.Nft>(_nfts);

            var total = 0;
            if (tokens is {Count: > 0})
            {
                var toFetch = tokens
                    .Where(item => item.Account.Data.Parsed.Info.TokenAmount.AmountUlong == 1)
                    .Where(item => nfts
                        .All(t => t.metaplexData.data.mint!= item.Account.Data.Parsed.Info.Mint)).ToArray();
                total = nfts.Count + toFetch.Length;
                
                foreach (var item in toFetch)
                {
                    if (Application.platform == RuntimePlatform.WebGLPlayer)
                    {
                        // If we are on WebGL, we need to add a min delay between requests
                        requestsMillisecondsDelay = Mathf.Max(requestsMillisecondsDelay, 100, NftLoadingRequestsDelay);
                    }
                    if (requestsMillisecondsDelay > 0) await UniTask.Delay(requestsMillisecondsDelay);
                    await UniTask.SwitchToMainThread();
                    var tNft = Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, Rpc, loadTexture: loadTexture).AsUniTask();
                    loadingTasks.Add(tNft);
                    tNft.ContinueWith(nft =>
                        {
                            if(tNft.AsTask().Exception != null || nft == null) {
                                total--;
                                return;
                            }
                            nfts.Add(nft);
                            if(notifyRegisteredListeners) 
                                OnNFTsUpdateInternal?.Invoke(nfts, total);
                        }).Forget();
                }
            }
            await UniTask.WhenAll(loadingTasks);
            OnNFTsUpdateInternal?.Invoke(nfts, nfts.Count);
            _nfts = nfts;
            return nfts;
        }

        private static async UniTask SubscribeToWalletEvents(Commitment commitment = Commitment.Processed)
        {
            if(WsRpc == null) return;
            await Wallet.AwaitWsRpcConnection();
            OnWebSocketConnect?.Invoke();
            await WsRpc.SubscribeAccountInfoAsync(
                Account.PublicKey,
                (_, accountInfo) =>
                {
                    Debug.Log("Account changed!, updated lamport: " + accountInfo.Value.Lamports);
                    _solAmount = accountInfo.Value.Lamports / 1000000000d;
                    OnBalanceChangeInternal?.Invoke(_solAmount);
                    UpdateNFTs(commitment: commitment).Forget();
                },
                commitment
            );
        }
        
        private IRpcClient GetDefaultRpc()
        {
            var inGame = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            return inGame.ActiveRpcClient;
        }
        
        private IStreamingRpcClient GetDefaultWsRpc()
        {
            var inGame = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            return inGame.ActiveStreamingRpcClient;
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
