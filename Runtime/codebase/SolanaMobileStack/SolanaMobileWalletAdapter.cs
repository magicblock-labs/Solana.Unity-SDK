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
                    string reauthPublicKey = null;
                    // TODO: change to using var after PR #260 merges (IDisposable not yet on LocalAssociationScenario)
                    var reauthorizeScenario = new LocalAssociationScenario();
                    var reauthorizeResult = await reauthorizeScenario.StartAndExecute(
                        new List<Action<IAdapterOperations>>
                        {
                            async client =>
                            {
                                var reauth = await client.Reauthorize(
                                    new Uri(_walletOptions.identityUri),
                                    new Uri(_walletOptions.iconUri, UriKind.Relative),
                                    _walletOptions.name, authToken);
                                if (reauth != null && !string.IsNullOrEmpty(reauth.AuthToken))
                                {
                                    _authToken = reauth.AuthToken;
                                    reauthPublicKey = reauth.PublicKey != null
                                        ? new PublicKey(reauth.PublicKey).ToString()
                                        : null;
                                }
                            }
                        }
                    );
                    if (reauthorizeResult.WasSuccessful)
                    {
                        if (string.IsNullOrEmpty(_authToken))
                        {
                            // Reauthorize RPC succeeded but wallet returned no token - treat as failure
                            // Fall through to cleanup below
                        }
                        else
                        {
                            PlayerPrefs.SetString("authToken", _authToken);
                            PlayerPrefs.Save();
                            var resolvedKey = !string.IsNullOrEmpty(reauthPublicKey) ? reauthPublicKey : pk;
                            return new Account(string.Empty, new PublicKey(resolvedKey));
                        }
                    }
                    // Reauthorize failed or returned empty token - clear cached credentials
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
            if (authorization == null)
            {
                throw new Exception("[MWA] Login: authorization was not populated");
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
            if (_authToken.IsNullOrEmpty() && _walletOptions.keepConnectionAlive)
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
            if (authorization == null)
            {
                throw new Exception("[MWA] SignAllTransactions: authorization was not populated");
            }
            if (res == null)
            {
                throw new Exception("[MWA] SignAllTransactions: signed payloads were not populated");
            }
            _authToken = authorization.AuthToken;
            if (_walletOptions.keepConnectionAlive)
            {
                PlayerPrefs.SetString("authToken", _authToken);
                PlayerPrefs.Save();
            }
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

            if (!authToken.IsNullOrEmpty())
            {
                try
                {
                    // TODO: change to using var after PR #260 merges (IDisposable not yet on LocalAssociationScenario)
                    var localAssociationScenario = new LocalAssociationScenario();
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
                        Debug.LogWarning($"[MWA] Deauthorize returned error: {result.Error.Message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MWA] Deauthorize transport failed (best-effort): {e}");
                }
            }

            Logout();
            OnWalletDisconnected?.Invoke();
        }

        public async Task ReconnectWallet()
        {
            try
            {
                var account = await Login();
                if (account != null)
                {
                    OnWalletReconnected?.Invoke();
                }
                else
                {
                    Debug.LogWarning("[MWA] ReconnectWallet: Login returned null, not firing OnWalletReconnected");
                    throw new Exception("ReconnectWallet failed: Login returned null");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }

        public async Task<CapabilitiesResult> GetCapabilities()
        {
            CapabilitiesResult capabilities = null;
            // TODO: change to using var after PR #260 merges (IDisposable not yet on LocalAssociationScenario)
            var localAssociationScenario = new LocalAssociationScenario();
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        capabilities = await client.GetCapabilities();
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }
            if (capabilities == null)
            {
                throw new Exception("[MWA] GetCapabilities RPC succeeded but returned no data");
            }
            return capabilities;
        }

        public override async Task<byte[]> SignMessage(byte[] message)
        {
            if (_authToken.IsNullOrEmpty() && _walletOptions.keepConnectionAlive)
                _authToken = PlayerPrefs.GetString("authToken", null);

            string cachedPk = Account?.PublicKey?.ToString()
                ?? PlayerPrefs.GetString("pk", null);
            if (string.IsNullOrEmpty(cachedPk))
                throw new Exception("[MWA] Cannot sign message: no account available");

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
                            addresses: new List<byte[]> { new PublicKey(cachedPk).KeyBytes }
                        );
                    }
                }
            );
            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }
            if (authorization == null)
            {
                throw new Exception("[MWA] SignMessage: authorization was not populated");
            }
            if (signedMessages == null)
            {
                throw new Exception("[MWA] SignMessage: signed payloads were not populated");
            }
            _authToken = authorization.AuthToken;
            if (_walletOptions.keepConnectionAlive)
            {
                PlayerPrefs.SetString("authToken", _authToken);
                PlayerPrefs.Save();
            }
            return signedMessages.SignedPayloadsBytes[0];
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in phantom wallet");
        }
    }
}
