using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOT;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class PhantomWebGL: WalletBase
    {
        
        private readonly PhantomWalletOptions _deepLinksWalletOptions;
        
        private static TaskCompletionSource<Account> _loginTaskCompletionSource;
        private static TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private static TaskCompletionSource<byte[]> _signedMessageTaskCompletionSource;        
        private static Transaction _currentTransaction;
        private static Account _account;

        public PhantomWebGL(
            PhantomWalletOptions deepLinksWalletOptions, 
            RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null,  bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            _deepLinksWalletOptions = deepLinksWalletOptions;
        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            ExternConnectPhantom(OnPhantomConnected);
            return _loginTaskCompletionSource.Task;
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            var encode = Encoders.Base58.EncodeData(transaction.CompileMessage());
            _currentTransaction = transaction;
            ExternSignTransaction(encode, OnTransactionSigned);
            return _signedTransactionTaskCompletionSource.Task;
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            _signedMessageTaskCompletionSource = new TaskCompletionSource<byte[]>();
            ExternSignMessage(Convert.ToBase64String(message), OnMessageSigned);
            return _signedMessageTaskCompletionSource.Task;
        }
        
        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }
        
        #region WebGL Callbacks
        
        /// <summary>
        /// Called from java script when the phantom wallet approves the connection
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnPhantomConnected(string walletPubKey)
        {
            _account = new Account("", walletPubKey);
            _loginTaskCompletionSource.SetResult(_account);
        }

        /// <summary>
        /// Called from java script when the phantom wallet signed the transaction and return the signature
        /// that we then need to put into the transaction before we send it out.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void OnTransactionSigned(string signature)
        {
            _currentTransaction.Signatures.Add(new SignaturePubKeyPair()
            {
                PublicKey = _account.PublicKey,
                Signature = Encoders.Base58.DecodeData(signature)
            });
            _signedTransactionTaskCompletionSource.SetResult(_currentTransaction);
        }

        /// <summary>
        /// Called from java script when the phantom wallet signed the message and return the signature.
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        public static void OnMessageSigned(string signature)
        {
            _signedMessageTaskCompletionSource.SetResult(Encoders.Base58.DecodeData(signature));
        }

        #endregion

        #if UNITY_WEBGL
        
        [DllImport("__Internal")]
        private static extern void ExternConnectPhantom(Action<string> callback);

        [DllImport("__Internal")]
        private static extern void ExternSignTransaction(string transaction, Action<string> callback);

        [DllImport("__Internal")]
        private static extern void ExternSignMessage(string message, Action<string> callback);        
        
        #else
        private static void ExternConnectPhantom(Action<string> callback){}
        private static void ExternSignTransaction(string transaction, Action<string> callback){}
        private static void ExternSignMessage(string message, Action<string> callback){}
        #endif
    }
}