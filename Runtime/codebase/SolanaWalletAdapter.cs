using System;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    
    [Serializable]
    public class SolanaWalletAdapterOptions
    {
        public SolanaMobileWalletAdapterOptions solanaMobileWalletAdapterOptions;
        public SolanaWalletAdapterWebGLOptions solanaWalletAdapterWebGLOptions;
        public PhantomWalletOptions phantomWalletOptions;
    }
    
    public class SolanaWalletAdapter: WalletBase
    {
        private readonly WalletBase _internalWallet;

        public SolanaWalletAdapter(SolanaWalletAdapterOptions options, RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            #if UNITY_ANDROID
            #pragma warning disable CS0618
            _internalWallet = new SolanaMobileWalletAdapter(options.solanaMobileWalletAdapterOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #elif UNITY_WEBGL
            #pragma warning disable CS0618
            _internalWallet = new SolanaWalletAdapterWebGL(options.solanaWalletAdapterWebGLOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #elif UNITY_IOS
            #pragma warning disable CS0618
            _internalWallet = new PhantomDeepLink(options.phantomWalletOptions, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            #else
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
            if (_internalWallet != null)
                return _internalWallet.SignAllTransactions(transactions);
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
            throw new NotImplementedException();
        }

        public override void Logout()
        {
            base.Logout();
            _internalWallet?.Logout();
        }
    }
}