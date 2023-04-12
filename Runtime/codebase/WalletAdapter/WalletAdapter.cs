using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
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
        
        public class WalletSpecs
        {
            public string name;
            public bool installed;
            public bool canSign;

            public override string ToString()
            {
                return $"{name}: installed? {installed}, can sign? {canSign}";
            }
        }

        public static WalletSpecs[] Wallets = new WalletSpecs[1];
        public static WalletSpecs CurrentWallet;
            

        public WalletAdapter(RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
           
        }
        
        public static void InitializeWallets() {
            Debug.Log("WalletAdapter Builder");
            Wallets[0] = new WalletSpecs(){name = "Phantom", installed = false, canSign = false};
            Debug.Log("WalletAdapter Wallets-> " + Wallets[0].ToString());
            Debug.Log("WalletAdapter Wallets-> " + Wallets[0].name);
            CurrentWallet = Wallets[0];
        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            try
            {
                ExternConnectWallet(CurrentWallet.name,OnWalletConnected);
            }
            catch (EntryPointNotFoundException)
            {
                _loginTaskCompletionSource.SetResult(null);
                return _loginTaskCompletionSource.Task;
            }
            return _loginTaskCompletionSource.Task;
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            Debug.Log("WalletAdapter SignTransaction -> wallet: " + CurrentWallet.name);
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            _currentTransaction = transaction;
            var base64TransactionStr = Convert.ToBase64String(transaction.Serialize()) ;
            Debug.Log("WalletAdapter SignTransaction base64TransactionStr-> " + base64TransactionStr);
            ExternSignTransactionWallet(CurrentWallet.name,base64TransactionStr, OnTransactionSigned);
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
                
        #else
                private static void ExternConnectWallet(string walletName, Action<string> callback){}
                private static void ExternSignTransactionWallet(string walletName, string transaction, Action<string> callback){}
                
        #endif
    }
}
