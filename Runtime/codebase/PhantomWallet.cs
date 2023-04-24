using System;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    
    [Serializable]
    public class PhantomWalletOptions
    {
        public string phantomApiVersion = "v1";
        public string appMetaDataUrl = "https://github.com/garbles-labs/Solana.Unity-SDK";
        public string deeplinkUrlScheme = "unitydl";
    }
    
    
    public class PhantomWallet : WalletBase
    {
        private readonly PhantomWalletOptions _phantomWalletOptions;
        
        private Transaction _currentTransaction;

        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private readonly WalletBase _internalWallet;

        public PhantomWallet(
            PhantomWalletOptions phantomWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet, 
            string customRpcUri = null, 
            string customStreamingRpcUri = null, 
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup
        ) 
        {
            _phantomWalletOptions = phantomWalletOptions;
            #if UNITY_IOS || UNITY_ANDROID
            _internalWallet = new PhantomDeepLink(phantomWalletOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #endif
            #if UNITY_WEBGL
            _internalWallet = new PhantomWebGL(phantomWalletOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #endif
        }

        protected override Task<Account> _Login(string password = null)
        {
            if (_internalWallet != null)
                return _internalWallet.Login(password);
            throw new NotImplementedException();
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            if (_internalWallet != null)
                return _internalWallet.SignTransaction(transaction);
            throw new NotImplementedException();
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            if (_internalWallet != null)
                return _internalWallet.SignMessage(message);
            throw new NotImplementedException();
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in phantom wallet");
        }
    }
}