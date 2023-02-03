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
    public class XNFTWallet: WalletBase
    {
        
        private static TaskCompletionSource<Account> _loginTaskCompletionSource;
        private static TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private static Transaction _currentTransaction;
        private static Account _account;

        public XNFTWallet(RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {

        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            try
            {
                ExternConnectXNFT(OnXNFTConnected);
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
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            var encode = Encoders.Base58.EncodeData(transaction.CompileMessage());
            _currentTransaction = transaction;
            ExternSignTransactionXNFT(encode, OnTransactionSigned);
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
        /// Called from java script when the xnft approves the connection
        /// </summary>
        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void OnXNFTConnected(string walletPubKey)
        {
            Debug.Log($"Wallet {walletPubKey} connected!");
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

        #endregion

        #if UNITY_WEBGL
        
        [DllImport("__Internal")]
        private static extern void ExternConnectXNFT(Action<string> callback);

        [DllImport("__Internal")]
        private static extern void ExternSignTransactionXNFT(string transaction, Action<string> callback);
        
        #else
        private static void ExternConnectXNFT(Action<string> callback){}
        private static void ExternSignTransactionXNFT(string transaction, Action<string> callback){}
        #endif
    }
}