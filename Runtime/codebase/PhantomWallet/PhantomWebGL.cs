using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class PhantomWebGL: WalletBase
    {
        
        private readonly PhantomWalletOptions _phantomWalletOptions;
        
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private Transaction _currentTransaction;

        public PhantomWebGL(
            PhantomWalletOptions phantomWalletOptions, 
            RpcCluster rpcCluster = RpcCluster.DevNet, string customRpc = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpc, autoConnectOnStartup)
        {
            _phantomWalletOptions = phantomWalletOptions;
        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            ExternConnectPhantom();
            return _loginTaskCompletionSource.Task;
        }

        public override Task<Transaction> SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            var encode = Encoders.Base58.EncodeData(transaction.CompileMessage());
            _currentTransaction = transaction;
            ExternSignTransaction(encode);
            return _signedTransactionTaskCompletionSource.Task;
        }
        
        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }
        
        #region WebGL Callbacks
        
        /// <summary>
        /// Called from java script when the phantom wallet approves the connection
        /// </summary>
        public void OnPhantomConnected(string walletPubKey)
        {
            Debug.Log($"Wallet {walletPubKey} connected!");
            _loginTaskCompletionSource.SetResult(new Account("", walletPubKey));
        }

        /// <summary>
        /// Called from java script when the phantom wallet signed the transaction and return the signature
        /// that we then need to put into the transaction before we send it out.
        /// </summary>
        public void OnTransactionSigned(string signature)
        {
            _currentTransaction.Signatures.Add(new SignaturePubKeyPair()
            {
                PublicKey = Account.PublicKey,
                Signature = Encoders.Base58.DecodeData(signature)
            });
            _signedTransactionTaskCompletionSource.SetResult(_currentTransaction);
        }

        #endregion

        #if UNITY_WEBGL
        
        [DllImport("__Internal")]
        private static extern void ExternConnectPhantom();

        [DllImport("__Internal")]
        private static extern void ExternSignTransaction(string transaction);
        
        #else
        private static extern void ExternConnectPhantom();
        private static extern void ExternSignTransaction(string transaction);
        #endif
    }
}