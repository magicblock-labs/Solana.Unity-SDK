using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    [Serializable]
    public class SolanaWalletAdapterWebGLOptions
    {
        public GameObject walletAdapterButtonPrefab;

        public GameObject walletAdapterUIPrefab;
    }
    public class SolanaWalletAdapterWebGL: WalletBase
    {
        private static SolanaWalletAdapterWebGLOptions _walletOptions;
        private static TaskCompletionSource<Account> _loginTaskCompletionSource;
        private static TaskCompletionSource<string> _getWalletsTaskCompletionSource;
        private static TaskCompletionSource<bool> _walletsInitializedTaskCompletionSource;
        private static TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private static TaskCompletionSource<Transaction[]> _signedAllTransactionsTaskCompletionSource;
        private static TaskCompletionSource<byte[]> _signedMessageTaskCompletionSource;
        private static Transaction[] _currentTransactions;
        private static Account _account;
        public static GameObject WalletAdapterUI { get; private set; }

        [Serializable]
        public class WalletSpecs
        {
            public string name;
            public bool installed;
            public string icon;
            public override string ToString()
            {
                return $"{name}: installed? {installed}";
            }
        }
        
        [Serializable]
        public class WalletSpecsObject
        {
            public WalletSpecs[] wallets;
        }

        
        public static WalletSpecs[] Wallets { get; private set; }

        private static WalletSpecs _currentWallet;

        private static string _clusterName;
            

        [Obsolete("Use SolanaWalletAdapter class instead, which is the cross platform wrapper.")]
        public SolanaWalletAdapterWebGL(
            SolanaWalletAdapterWebGLOptions solanaWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null,
            string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            _walletOptions = solanaWalletOptions;
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                throw new Exception("SolanaWalletAdapterWebGL can only be used on WebGL");
            }
            _clusterName = RPCNameMap[(int)RpcCluster];
        }
        
        private static async Task InitWallets() {
            _currentWallet = null;
            _walletsInitializedTaskCompletionSource = new TaskCompletionSource<bool>();
            
            InitWalletAdapter(OnWalletsInitialized, _clusterName);
            bool isXnft = await _walletsInitializedTaskCompletionSource.Task;
            if (isXnft){
               _currentWallet = new WalletSpecs()
               {
                   name = "XNFT",
                   icon = "",
                   installed = true
               };
            } else{
                _getWalletsTaskCompletionSource = new TaskCompletionSource<string>();
                ExternGetWallets(OnWalletsLoaded);
                var walletsData = await _getWalletsTaskCompletionSource.Task;
                Wallets = JsonUtility.FromJson<WalletSpecsObject>(walletsData).wallets;
            }
        }
        
        
        /// <summary>
        /// Check whether it's an XNFT or not
        /// </summary>
        /// <returns> true if it's an XNFT, false otherwise</returns>
        public static async Task<bool> IsXnft(){
            if(RuntimePlatform.WebGLPlayer != Application.platform){
                return false;
            }
            await InitWallets();
            return _currentWallet != null && _currentWallet.name == "XNFT";
        }

        protected override async Task<Account> _Login(string password = null)
        {
            await SetCurrentWallet();
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            try
            {
                ExternConnectWallet(_currentWallet.name, OnWalletConnected);
            }
            catch (Exception e)
            {
                Debug.LogError("WalletAdapter _Login -> Exception: " + e);
                _loginTaskCompletionSource.SetResult(null);
            }
            if (WalletAdapterUI != null ){
                WalletAdapterUI.SetActive(false);
            }
            return await _loginTaskCompletionSource.Task;
        }
        
        private static async Task SetCurrentWallet()
        {
            await InitWallets();
            if (_currentWallet == null)
            {
                if (WalletAdapterUI == null)
                {
                    WalletAdapterUI = GameObject.Instantiate(_walletOptions.walletAdapterUIPrefab);
                }

                var waitForWalletSelectionTask = new TaskCompletionSource<string>();
                var walletAdapterScreen =
                    WalletAdapterUI.transform.GetChild(0).gameObject.GetComponent<WalletAdapterScreen>();
                walletAdapterScreen.viewPortContent = WalletAdapterUI.transform.GetChild(0).Find("Scroll View")
                    .Find("Viewport").Find("Content").GetComponent<RectTransform>();
                walletAdapterScreen.buttonPrefab = _walletOptions.walletAdapterButtonPrefab;
                walletAdapterScreen.OnSelectedAction = walletName =>
                {
                    waitForWalletSelectionTask.SetResult(walletName);
                };
                WalletAdapterUI.SetActive(true);
                var walletName = await waitForWalletSelectionTask.Task;
                _currentWallet = Array.Find(Wallets, wallet => wallet.name == walletName);
            }
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            var base64TransactionStr = Convert.ToBase64String(transaction.Serialize()) ;
            ExternSignTransactionWallet(_currentWallet.name,base64TransactionStr, OnTransactionSigned);
            return _signedTransactionTaskCompletionSource.Task;
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            _signedMessageTaskCompletionSource = new TaskCompletionSource<byte[]>();
            var base64MessageStr = Convert.ToBase64String(message) ;
            ExternSignMessageWallet(_currentWallet.name, base64MessageStr, OnMessageSigned);
            return _signedMessageTaskCompletionSource.Task;
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            _signedAllTransactionsTaskCompletionSource = new TaskCompletionSource<Transaction[]>();
            _currentTransactions = transactions;
            string[] base64Transactions = new string[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                base64Transactions[i] = Convert.ToBase64String(transactions[i].Serialize());
            }
            var base64TransactionsStr = string.Join(",", base64Transactions);
            ExternSignAllTransactionsWallet(_currentWallet.name,base64TransactionsStr, OnAllTransactionsSigned);
            return _signedAllTransactionsTaskCompletionSource.Task;
        }

        #region WebGL Callbacks
        
        /// <summary>
        /// Called from javascript when the wallet adapter approves the connection
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnWalletConnected(string walletPubKey)
        {
            if (walletPubKey == null)
            {
                _loginTaskCompletionSource.TrySetException(new Exception("Login cancelled"));
                _loginTaskCompletionSource.TrySetResult(null);
                return;
            }
            Debug.Log($"Wallet {walletPubKey} connected!");
            _account = new Account("", walletPubKey);
            _loginTaskCompletionSource.TrySetResult(_account);
        }

        /// <summary>
        /// Called from javascript when the wallet signed the transaction and return the signature
        /// that we then need to put into the transaction before we send it out.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void OnTransactionSigned(string transaction)
        {
            if (transaction == null)
            {
                _signedTransactionTaskCompletionSource.TrySetException(new Exception("Transaction signing cancelled"));
                _signedTransactionTaskCompletionSource.TrySetResult(null);
                return;
            }
            var tx = Transaction.Deserialize(Convert.FromBase64String(transaction));
            _signedTransactionTaskCompletionSource.SetResult(tx);
        }
        
        /// <summary>
        /// Called from javascript when the wallet signed all transactions and return the signature
        /// that we then need to put into the transaction before we send it out.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void OnAllTransactionsSigned(string signatures)
        {
            if (signatures == null)
            {
                _signedAllTransactionsTaskCompletionSource.TrySetException(new Exception("Transactions signing cancelled"));
                _signedAllTransactionsTaskCompletionSource.TrySetResult(null);
                return;
            }
            string[] signaturesList = signatures.Split(',');
            for (int i = 0; i < signaturesList.Length; i++)
            {
                _currentTransactions[i].Signatures.Add(new SignaturePubKeyPair()
                {
                    PublicKey = _account.PublicKey,
                    Signature = Convert.FromBase64String(signaturesList[i])
                });
            }
            _signedAllTransactionsTaskCompletionSource.SetResult(_currentTransactions);
        }
        
        
        
        /// <summary>
        /// Called from javascript when the wallet adapter signed the message and return the signature.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void OnMessageSigned(string signature)
        {
            if (signature == null)
            {
                _signedMessageTaskCompletionSource.TrySetException(new Exception("Message signing cancelled"));
                _signedMessageTaskCompletionSource.TrySetResult(null);
                return;
            }
            _signedMessageTaskCompletionSource.SetResult(Convert.FromBase64String(signature));
        }

        /// <summary>
        /// Called from javascript when the wallet adapter script is loaded. Returns whether it's an XNFT or not.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<bool>))]
        private static void OnWalletsInitialized(bool isXnft)
        {
            _walletsInitializedTaskCompletionSource.SetResult(isXnft);
        }
        
        /// <summary>
        /// Called from javascript when the wallets are loaded
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnWalletsLoaded(string walletsData)
        {
            _getWalletsTaskCompletionSource.SetResult(walletsData);
        }

        #endregion

        #if UNITY_WEBGL
                
                [DllImport("__Internal")]
                private static extern void ExternConnectWallet(string walletName,Action<string> callback);

                [DllImport("__Internal")]
                private static extern void ExternSignTransactionWallet(string walletName, string transaction, Action<string> callback);
                
                [DllImport("__Internal")]
                private static extern void ExternSignAllTransactionsWallet(string walletName, string transactions, Action<string> callback);

                [DllImport("__Internal")]
                private static extern void ExternSignMessageWallet(string walletName, string messageBase64, Action<string> callback);
        
                
                [DllImport("__Internal")]
                private static extern string  ExternGetWallets(Action<string> callback);

                [DllImport("__Internal")]
                private static extern void InitWalletAdapter(Action<bool> callback, string clusterName);
                
                
        #else
                private static void ExternConnectWallet(string walletName, Action<string> callback){}
                private static void ExternSignTransactionWallet(string walletName, string transaction, Action<string> callback){}
                private static void ExternSignAllTransactionsWallet(string walletName, string transactions, Action<string> callback){}
                private static void ExternSignMessageWallet(string walletName, string messageBase64, Action<string> callback){}
                private static string ExternGetWallets(Action<string> callback){return null;}
                private static void InitWalletAdapter(Action<bool> callback, string clusterName){}
                
        #endif
    }
}
