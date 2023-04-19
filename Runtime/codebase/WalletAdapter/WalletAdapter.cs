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
    public class WalletAdapter: WalletBase
    {

        private static TaskCompletionSource<Account> _loginTaskCompletionSource;
        private static TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private static Transaction _currentTransaction;
        private static Account _account;
        private static GameObject _walletAdapterUI;
        
        [Serializable]
        public class WalletSpecs
        {
            public string name;
            public bool installed;

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
            

        public WalletAdapter(RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            InitWallets();
        }
        
        private static void InitWallets() {
            Debug.Log("InitWallets");
            # if UNITY_WEBGL && !UNITY_EDITOR
            InitWalletAdapter();
            var walletsData = ExternGetWallets();
            # else
            var walletsData = "{\"wallets\":[{\"name\":\"Phantom\",\"installed\":true},{\"name\":\"Solflare\",\"installed\":true},{\"name\":\"Sollet\",\"installed\":true},{\"name\":\"Sollet.io\",\"installed\":true},{\"name\":\"Math Wallet\",\"installed\":true},{\"name\":\"Token Pocket\",\"installed\":true},{\"name\":\"Ledger\",\"installed\":true},{\"name\":\"Torus\",\"installed\":true},{\"name\":\"Anchor\",\"installed\":true}]}\n";
            # endif
            Debug.Log("WalletAdapter walletsData-> " + walletsData);
            Wallets = JsonUtility.FromJson<WalletSpecsObject>(walletsData).wallets;
            Debug.Log("WalletAdapter Wallets-> " + Wallets);
            

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
            _walletAdapterUI.SetActive(false);
            return await _loginTaskCompletionSource.Task;
        }
        
        private static async Task SetCurrentWallet()
        {
            if (_walletAdapterUI == null)
            {
                GameObject walletAdapterUIPrefab = Resources.Load<GameObject>("SolanaUnitySDK/WalletAdapterUI");
                _walletAdapterUI = GameObject.Instantiate(walletAdapterUIPrefab);
            }
            else
            {
                _walletAdapterUI.SetActive(true);
            }
            
            var waitForWalletSelectionTask = new TaskCompletionSource<string>();
            var walletAdapterScreen = _walletAdapterUI.transform.GetChild(0).gameObject.GetComponent<WalletAdapterScreen>();
            walletAdapterScreen.OnSelectedAction = walletName =>
            {
                Debug.Log("WalletAdapter OnSelectedAction -> walletName: " + walletName);
                waitForWalletSelectionTask.SetResult(walletName);
                Debug.Log("WalletAdapter OnSelectedAction - after SetResult");
            };
            var walletName = await waitForWalletSelectionTask.Task;
            Debug.Log("WalletAdapter after waitForWalletSelectionTask -> walletName: " + walletName);
            _currentWallet = Array.Find(Wallets, wallet => wallet.name == walletName);
            Debug.Log("WalletAdapter after Array.Find -> _currentWallet.name: " + _currentWallet.name);
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            Debug.Log("WalletAdapter SignTransaction -> wallet: " + _currentWallet.name);
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            _currentTransaction = transaction;
            var base64TransactionStr = Convert.ToBase64String(transaction.Serialize()) ;
            Debug.Log("WalletAdapter SignTransaction base64TransactionStr-> " + base64TransactionStr);
            ExternSignTransactionWallet(_currentWallet.name,base64TransactionStr, OnTransactionSigned);
            return _signedTransactionTaskCompletionSource.Task;
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            throw new NotImplementedException();
        }
        
        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }
        
        #region WebGL Callbacks
        
        /// <summary>
        /// Called from javascript when the wallet adapter approves the connection
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnWalletConnected(string walletPubKey)
        {
            Debug.Log($"Wallet {walletPubKey} connected!");
            _account = new Account("", walletPubKey);
            _loginTaskCompletionSource.SetResult(_account);
        }

        /// <summary>
        /// Called from javascript when the wallet signed the transaction and return the signature
        /// that we then need to put into the transaction before we send it out.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void OnTransactionSigned(string signature)
        {
            Debug.Log($"OnTransactionSigned -> signature {signature}");
            _currentTransaction.Signatures.Add(new SignaturePubKeyPair()
            {
                PublicKey = _account.PublicKey,
                Signature = Convert.FromBase64String(signature)
            });
            _signedTransactionTaskCompletionSource.SetResult(_currentTransaction);
        }

        #endregion

        #if UNITY_WEBGL
                
                [DllImport("__Internal")]
                private static extern void ExternConnectWallet(string walletName,Action<string> callback);

                [DllImport("__Internal")]
                private static extern void ExternSignTransactionWallet(string walletName, string transaction, Action<string> callback);
        
                [DllImport("__Internal")]
                private static extern string ExternGetWallets();

                [DllImport("__Internal")]
                private static extern void InitWalletAdapter();
                
                
        #else
                private static void ExternConnectWallet(string walletName, Action<string> callback){}
                private static void ExternSignTransactionWallet(string walletName, string transaction, Action<string> callback){}
                private static string ExternGetWallets(){}
                
        #endif
    }
}
