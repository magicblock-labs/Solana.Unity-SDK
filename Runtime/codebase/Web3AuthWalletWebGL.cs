using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class Web3AuthWalletWebGL : WalletBase
    {
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private readonly Web3AuthWalletOptions _web3AuthWalletOptions;
        private Provider _loginProvider = Provider.GOOGLE;
        
        private readonly Dictionary<int, Web3Auth.Network> _rpcClusterMap = new()
        {
            { 0, Web3Auth.Network.MAINNET },
            { 1, Web3Auth.Network.TESTNET},
            { 2, Web3Auth.Network.TESTNET},
            { 3, Web3Auth.Network.TESTNET}
        };
        
        public Web3AuthWalletWebGL(Web3AuthWalletOptions web3AuthWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpc = null,
            bool autoConnectOnStartup = false
            ) : base(rpcCluster, customRpc, autoConnectOnStartup)
        {
            _web3AuthWalletOptions = web3AuthWalletOptions;
            #if UNITY_WEBGL && ! UNITY_EDITOR
            InitWeb3Auth(_web3AuthWalletOptions.clientId);
            #endif
        }

        protected override Task<Account> _Login(string password = null)
        {
            var options = new LoginParams
            {
                loginProvider = _loginProvider
            };
            #if UNITY_WEBGL && ! UNITY_EDITOR
            LoginWeb3Auth();
            #endif
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            return _loginTaskCompletionSource.Task;
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            return _Login(password);
        }
        public override Task<Transaction> SignTransaction(Transaction transaction)
        {
            transaction.Sign(Account);
            return Task.FromResult(transaction);
        }
        
        public Task<Account> LoginWithProvider(Provider provider)
        {
            _loginProvider = provider;
            return Login();
        }

        #if UNITY_WEBGL && ! UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void InitWeb3Auth(string clientId);
        #endif
        
        #if UNITY_WEBGL && ! UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void LoginWeb3Auth();
        #endif
    }
}
