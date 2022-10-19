using System;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    
    [Serializable]
    public class Web3AuthWalletOptions
    {
        public string appName = "Web3Auth Sample App";
        public string logoLight;
        public string logoDark;
        public string defaultLanguage = "en";
        public bool dark = true;
        public string themeName = "primary";
        public string themeColor = "#123456";
        public string redirectUrl = "torusapp://com.torus.Web3AuthUnity/auth";
        public string clientId = "BAwFgL-r7wzQKmtcdiz2uHJKNZdK7gzEf2q-m55xfzSZOw8jLOyIi4AVvvzaEQO5nv2dFLEmf9LBkF8kaq3aErg";
    }
    
    
    public class Web3AuthWallet : WalletBase
    {
        private readonly Web3Auth _web3Auth;
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private readonly WalletBase _internalWallet;

        public Web3AuthWallet(
            Web3AuthWalletOptions web3AuthWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpc = null,
            bool autoConnectOnStartup = false
        ) : base(rpcCluster, customRpc, autoConnectOnStartup)
        {
            #if UNITY_WEBGL && ! UNITY_EDITOR
            _internalWallet = new Web3AuthWalletWebGL(web3AuthWalletOptions, rpcCluster, customRpc, autoConnectOnStartup);
            #else
            _internalWallet = new Web3AuthWalletBase(web3AuthWalletOptions, rpcCluster, customRpc, autoConnectOnStartup);
            #endif
        }

        protected override Task<Account> _Login(string password = null)
        {
            if (_internalWallet != null)
                return _internalWallet.Login(password);
            throw new NotImplementedException();
        }
        
        public override Task<Transaction> SignTransaction(Transaction transaction)
        {
            if (_internalWallet != null)
                return _internalWallet.SignTransaction(transaction);
            throw new NotImplementedException();
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            if (_internalWallet != null)
                return _internalWallet.CreateAccount(mnemonic, password);
            throw new NotImplementedException();
        }
        
        public Task<Account> LoginWithProvider(Provider provider)
        {
            if (_internalWallet != null)
                #if UNITY_WEBGL && ! UNITY_EDITOR
                return ((Web3AuthWalletWebGL)_internalWallet).LoginWithProvider(provider);
                #else
                return ((Web3AuthWalletBase)_internalWallet).LoginWithProvider(provider);
                #endif
            throw new NotImplementedException();
        }
    }
}