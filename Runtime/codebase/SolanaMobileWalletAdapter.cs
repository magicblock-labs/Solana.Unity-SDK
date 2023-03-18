using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using SolanaMobileStack;
using SolanaMobileStack.Interfaces;
using SolanaMobileStack.JsonRpcClient.Responses;
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
    }
    
    
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
            return new Account(string.Empty, publicKey);
        }

        protected override async Task<Transaction> _SignTransaction(Transaction transaction)
        {
            if (_authToken.IsNullOrEmpty())
                throw new Exception("Authentication required");
            SignedResult res = null;
            var localAssociationScenario = new LocalAssociationScenario();
            AuthorizationResult reauthorization = null;
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        reauthorization = await client.Reauthorize(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, _authToken);
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
            _authToken = reauthorization.AuthToken;
            return Transaction.Deserialize(res.SignedPayloads[0]);
        }

        public override async Task<byte[]> SignMessage(byte[] message)
        {
            if (_authToken.IsNullOrEmpty())
                throw new Exception("Authentication required");
            SignedResult signedMessages = null;
            var localAssociationScenario = new LocalAssociationScenario();
            AuthorizationResult reauthorization = null;
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        reauthorization = await client.Reauthorize(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, _authToken);
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
            _authToken = reauthorization.AuthToken;
            return signedMessages.SignedPayloadsBytes[0];
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in phantom wallet");
        }
    }
}