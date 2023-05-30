using System;
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
                if (!pk.IsNullOrEmpty()) return new Account(string.Empty, new PublicKey(pk));
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
            }
            return new Account(string.Empty, publicKey);
        }

        protected override async Task<Transaction> _SignTransaction(Transaction transaction)
        {
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
                        res = await client.SignTransactions(new List<byte[]>
                        {
                            transaction.Serialize()
                        });
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }
            _authToken = authorization.AuthToken;
            return Transaction.Deserialize(res.SignedPayloads[0]);
        }


        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            throw new NotImplementedException();
        }

        public override void Logout()
        {
            base.Logout();
            PlayerPrefs.DeleteKey("pk");
            PlayerPrefs.Save();
        }

        public override async Task<byte[]> SignMessage(byte[] message)
        {
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
