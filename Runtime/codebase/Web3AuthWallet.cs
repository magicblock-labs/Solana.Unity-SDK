using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Merkator.Tools;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;

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
        public Web3Auth.Network network = Web3Auth.Network.TESTNET;
    }
    
    public class Web3AuthWallet : WalletBase
    {
        private readonly Web3Auth _web3Auth;
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private readonly Web3AuthWalletOptions _web3AuthWalletOptions;
        private Provider _loginProvider = Provider.GOOGLE;
        private TaskCompletionSource<Web3AuthResponse> _taskCompletionSource;
        
        public event Action<Account> OnLoginNotify;

        public Web3AuthWallet(Web3AuthWalletOptions web3AuthWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null,
            string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false
            ) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            _web3AuthWalletOptions = web3AuthWalletOptions;
            var gameObject = new GameObject("Web3Auth");
            _web3Auth = gameObject.AddComponent<Web3Auth>();
            var web3AuthOptions = new Web3AuthOptions
            {
                redirectUrl = new Uri(_web3AuthWalletOptions.redirectUrl),
                clientId = _web3AuthWalletOptions.clientId,
                network = _web3AuthWalletOptions.network,
                whiteLabel = new WhiteLabelData()
                {
                    name = _web3AuthWalletOptions.appName,
                    logoLight = _web3AuthWalletOptions.logoLight,
                    logoDark = _web3AuthWalletOptions.logoDark,
                    defaultLanguage = _web3AuthWalletOptions.defaultLanguage,
                    dark = _web3AuthWalletOptions.dark,
                    theme = new Dictionary<string, string>
                    {
                        {
                            _web3AuthWalletOptions.themeName,
                            _web3AuthWalletOptions.themeColor
                        }
                    }
                }
            };
            _web3Auth.setOptions(web3AuthOptions);
            _web3Auth.onLogin += OnLogin;
        }

        private void OnLogin(Web3AuthResponse response)
        {
            var keyBytes = ArrayHelpers.SubArray(Convert.FromBase64String(response.ed25519PrivKey), 0, 64);
            var wallet = new Wallet.Wallet(keyBytes);
            
            if (_loginTaskCompletionSource != null)
            {
                _loginTaskCompletionSource?.SetResult(wallet.Account);
            }
            else
            {   
                Account = wallet.Account;
                OnLoginNotify?.Invoke(wallet.Account);
            }
        }

        protected override Task<Account> _Login(string password = null)
        {
            if (Account != null)
                return Task.FromResult(Account);
            var options = new LoginParams
            {
                loginProvider = _loginProvider
            };
            _web3Auth.login(options);
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            return _loginTaskCompletionSource.Task;
        }
        
        public override void Logout()
        {
            base.Logout();
            _web3Auth.onLogin -= OnLogin;
            _web3Auth.logout();
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            return _Login(password);
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            transaction.Sign(Account);
            return Task.FromResult(transaction);
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            return Task.FromResult(Account.Sign(message));
        }
        
        public Task<Account> LoginWithProvider(Provider provider)
        {
            _loginProvider = provider;
            return Login();
        }
    }
}
