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
    public class Web3AuthWalletBase : WalletBase
    {
        private readonly Web3Auth _web3Auth;
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
        
        public Web3AuthWalletBase(Web3AuthWalletOptions web3AuthWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpc = null,
            bool autoConnectOnStartup = false
            ) : base(rpcCluster, customRpc, autoConnectOnStartup)
        {
            _web3AuthWalletOptions = web3AuthWalletOptions;
            var gameObject = new GameObject("Web3Auth");
            _web3Auth = gameObject.AddComponent<Web3Auth>();
            var web3AuthOptions = new Web3AuthOptions
            {
                redirectUrl = new Uri(_web3AuthWalletOptions.redirectUrl),
                clientId = _web3AuthWalletOptions.clientId,
                network = _rpcClusterMap[(int)rpcCluster],
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
            _loginTaskCompletionSource.SetResult(wallet.Account);
        }

        protected override Task<Account> _Login(string password = null)
        {
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
    }
}
