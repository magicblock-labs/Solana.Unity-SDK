using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Merkator.Tools;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
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
        public Web3Auth.Language defaultLanguage = Web3Auth.Language.en;
        public Web3Auth.ThemeModes mode = Web3Auth.ThemeModes.auto;
        public string themeName = "primary";
        public string themeColor = "#123456";
        public string redirectUrl = "torusapp://com.torus.Web3AuthUnity/auth";
        public string clientId = "BAwFgL-r7wzQKmtcdiz2uHJKNZdK7gzEf2q-m55xfzSZOw8jLOyIi4AVvvzaEQO5nv2dFLEmf9LBkF8kaq3aErg";
        public Web3Auth.Network network = Web3Auth.Network.TESTNET;
        public List<LoginConfig> loginConfig = null;
    }
    
    [Serializable]
    public class LoginConfig
    {
        public string verifier = "**-google-auth";
        public TypeOfLogin typeOfLogin = TypeOfLogin.GOOGLE;
        public string name = "google";
        public string description;
        public string clientId = "1243-[...]";
        public string verifierSubIdentifier;
        public string logoHover;
        public string logoLight;
        public string logoDark;
        public bool mainOption = false;
        public bool showOnModal = true;
        public bool showOnDesktop = true;
        public bool showOnMobile = true;
    }
    
    public class Web3AuthWallet : WalletBase
    {
        private readonly Web3Auth _web3Auth;
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private readonly Web3AuthWalletOptions _web3AuthWalletOptions;
        private Provider _loginProvider = Provider.GOOGLE;
        private LoginParams _loginParameters;
        private TaskCompletionSource<Web3AuthResponse> _taskCompletionSource;
        
        public event Action<Account> OnLoginNotify;
        public UserInfo userInfo;

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
                    appName = _web3AuthWalletOptions.appName,
                    logoLight = _web3AuthWalletOptions.logoLight,
                    logoDark = _web3AuthWalletOptions.logoDark,
                    defaultLanguage = _web3AuthWalletOptions.defaultLanguage,
                    mode = _web3AuthWalletOptions.mode,
                    theme = new Dictionary<string, string>
                    {
                        {
                            _web3AuthWalletOptions.themeName,
                            _web3AuthWalletOptions.themeColor
                        }
                    },
                }
            };
            if(_web3AuthWalletOptions.loginConfig is { Count: > 0 })
                web3AuthOptions.loginConfig = BuildLoginConfigDictionary(_web3AuthWalletOptions.loginConfig);
            _web3Auth.setOptions(web3AuthOptions);
            _web3Auth.onLogin += OnLogin;
        }

        private void OnLogin(Web3AuthResponse response)
        {
            userInfo = response.userInfo;
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
            if (_loginParameters != null)
            {
                options = _loginParameters;
            }
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
            foreach (var transaction in transactions)
            {
                transaction.PartialSign(Account);
            }
            return Task.FromResult(transactions);
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
        
        public Task<Account> LoginWithParams(LoginParams loginParams)
        {
            _loginParameters = loginParams;
            return Login();
        }

        #region Utils

        public Dictionary<string, LoginConfigItem> BuildLoginConfigDictionary(List<LoginConfig> loginConfigList) {
            if (loginConfigList == null) return null;
            var dictionary = new Dictionary<string, LoginConfigItem>();

            foreach (var config in loginConfigList) {
                var loginConfigItem = new LoginConfigItem {
                    verifier = config.verifier,
                    typeOfLogin = config.typeOfLogin,
                    name = config.name,
                    description = config.description,
                    clientId = config.clientId,
                    verifierSubIdentifier = config.verifierSubIdentifier,
                    logoHover = config.logoHover,
                    logoLight = config.logoLight,
                    logoDark = config.logoDark,
                    mainOption = config.mainOption,
                    showOnModal = config.showOnModal,
                    showOnDesktop = config.showOnDesktop,
                    showOnMobile = config.showOnMobile
                };

                dictionary.Add(config.name, loginConfigItem);
            }

            return dictionary;
        }

        #endregion
    }
}
