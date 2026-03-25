using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using UnityEngine;
using WebSocketSharp;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    
    [Serializable]
    public class SolanaMobileWalletAdapterOptions
    {
        public string identityUri = "https://solana.unity-sdk.gg/";
        public string iconUri = "/favicon.ico";
        public string name = "Solana.Unity-SDK";
        public bool keepConnectionAlive = true;
    }
    
    
    [Obsolete("Use SolanaWalletAdapter class instead, which is the cross platform wrapper.")]
    public class SolanaMobileWalletAdapter : WalletBase
    {
        private readonly SolanaMobileWalletAdapterOptions _walletOptions;
        
        private Transaction _currentTransaction;

        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private readonly WalletBase _internalWallet;
        private string _authToken;

        public event Action OnWalletDisconnected;
        public event Action OnWalletReconnected;

        public SolanaMobileWalletAdapter(
            SolanaMobileWalletAdapterOptions solanaWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet, 
            string customRpcUri = null, 
            string customStreamingRpcUri = null, 
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup
        )
        {
            _walletOptions = solanaWalletOptions;
            if (Application.platform != RuntimePlatform.Android)
            {
                throw new Exception("SolanaMobileWalletAdapter can only be used on Android");
            }
        }

        protected override async Task<Account> _Login(string password = null)
        {
            if (_walletOptions.keepConnectionAlive)
            {
                string pk = PlayerPrefs.GetString("pk", null);
                string authToken = PlayerPrefs.GetString("authToken", null);
                if (!pk.IsNullOrEmpty() && !authToken.IsNullOrEmpty())
                {
                    using var reauthorizeScenario = new LocalAssociationScenario();
                    var reauthorizeResult = await reauthorizeScenario.StartAndExecute(
                        new List<Action<IAdapterOperations>>
                        {
                            async client =>
                            {
                                var reauth = await client.Reauthorize(
                                    new Uri(_walletOptions.identityUri),
                                    new Uri(_walletOptions.iconUri, UriKind.Relative),
                                    _walletOptions.name, authToken);
                                _authToken = reauth.AuthToken;
                            }
                        }
                    );
                    if (reauthorizeResult.WasSuccessful)
                    {
                        return new Account(string.Empty, new PublicKey(pk));
                    }
                    PlayerPrefs.DeleteKey("pk");
                    PlayerPrefs.DeleteKey("authToken");
                    PlayerPrefs.Save();
                }
                else if (!pk.IsNullOrEmpty())
                {
                    PlayerPrefs.DeleteKey("pk");
                    PlayerPrefs.Save();
                }
            }
            AuthorizationResult authorization = null;
            var localAssociationScenario = new LocalAssociationScenario();
            var cluster = RPCNameMap[(int)RpcCluster];
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        authorization = await client.Authorize(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, cluster);
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }
            _authToken = authorization.AuthToken;
            var publicKey = new PublicKey(authorization.PublicKey);
            if (_walletOptions.keepConnectionAlive)
            {
                PlayerPrefs.SetString("pk", publicKey.ToString());
                PlayerPrefs.SetString("authToken", _authToken);
                PlayerPrefs.Save();
            }
            return new Account(string.Empty, publicKey);
        }

        protected override async Task<Transaction> _SignTransaction(Transaction transaction)
        {
            var result = await _SignAllTransactions(new Transaction[] { transaction });
            return result[0];
        }


        protected override async Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            if (_authToken.IsNullOrEmpty())
                _authToken = PlayerPrefs.GetString("authToken", null);

            var cluster = RPCNameMap[(int)RpcCluster];
            SignedResult res = null;
            var localAssociationScenario = new LocalAssociationScenario();
            AuthorizationResult authorization = null;
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        if (_authToken.IsNullOrEmpty())
                        {
                            authorization = await client.Authorize(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name, cluster);
                        }
                        else
                        {
                            authorization = await client.Reauthorize(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name, _authToken);   
                        }
                    },
                    async client =>
                    {
                        res = await client.SignTransactions(transactions.Select(transaction => transaction.Serialize()).ToList());
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }
            _authToken = authorization.AuthToken;
            return res.SignedPayloads.Select(transaction => Transaction.Deserialize(transaction)).ToArray();
        }


        public override void Logout()
        {
            base.Logout();
            PlayerPrefs.DeleteKey("pk");
            _authToken = null;
            PlayerPrefs.DeleteKey("authToken");
            PlayerPrefs.Save();
        }

        public async Task DisconnectWallet()
        {
            string authToken = _authToken;
            if (authToken.IsNullOrEmpty())
                authToken = PlayerPrefs.GetString("authToken", null);

            if (authToken.IsNullOrEmpty())
            {
                Logout();
                OnWalletDisconnected?.Invoke();
                return;
            }

            using var localAssociationScenario = new LocalAssociationScenario();
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        await client.Deauthorize(authToken);
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
            }
            Logout();
            OnWalletDisconnected?.Invoke();
        }

        public async Task ReconnectWallet()
        {
            try
            {
                await Login();
                OnWalletReconnected?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public override async Task<byte[]> SignMessage(byte[] message)
        {
            if (_authToken.IsNullOrEmpty())
                _authToken = PlayerPrefs.GetString("authToken", null);

            SignedResult signedMessages = null;
            var localAssociationScenario = new LocalAssociationScenario();
            AuthorizationResult authorization = null;
            var cluster = RPCNameMap[(int)RpcCluster];
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        if (_authToken.IsNullOrEmpty())
                        {
                            authorization = await client.Authorize(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name, cluster);
                        }
                        else
                        {
                            authorization = await client.Reauthorize(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name, _authToken);   
                        }
                    },
                    async client =>
                    {
                        signedMessages = await client.SignMessages(
                            messages: new List<byte[]> { message },
                            addresses: new List<byte[]> { Account.PublicKey.KeyBytes }
                        );
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }
            _authToken = authorization.AuthToken;
            return signedMessages.SignedPayloadsBytes[0];
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in phantom wallet");
        }
    }
}
