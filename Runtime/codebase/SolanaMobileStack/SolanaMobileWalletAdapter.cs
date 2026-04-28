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
        private const string PrefKeyPublicKey = "solana_sdk.mwa.public_key";
        // Single source of truth lives in PlayerPrefsAuthCache.DefaultKey.
        // Kept here as a private alias because the legacy key migration
        // below still touches PlayerPrefs directly. Live reads and writes
        // for the auth token go through _authCache (see IMwaAuthCache).
        private const string PrefKeyAuthToken = PlayerPrefsAuthCache.DefaultKey;
        
        private readonly SolanaMobileWalletAdapterOptions _walletOptions;
        
        private Transaction _currentTransaction;

        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private readonly WalletBase _internalWallet;
        private readonly IMwaAuthCache _authCache;
        private string _authToken;

        public event Action OnWalletDisconnected;
        public event Action OnWalletReconnected;

        public SolanaMobileWalletAdapter(
            SolanaMobileWalletAdapterOptions solanaWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet, 
            string customRpcUri = null, 
            string customStreamingRpcUri = null, 
            bool autoConnectOnStartup = false,
            IMwaAuthCache authCache = null) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup
        )
        {
            _walletOptions = solanaWalletOptions;
            if (Application.platform != RuntimePlatform.Android)
            {
                throw new Exception("SolanaMobileWalletAdapter can only be used on Android");
            }
            _authCache = authCache ?? new PlayerPrefsAuthCache();
            MigrateLegacyPrefKeys();
        }

        private static void MigrateLegacyPrefKeys()
        {
            const string legacyPk = "pk";
            const string legacyAuthToken = "authToken";

            if (!PlayerPrefs.HasKey(legacyPk) && !PlayerPrefs.HasKey(legacyAuthToken))
                return;

            if (PlayerPrefs.HasKey(legacyPk) && !PlayerPrefs.HasKey(PrefKeyPublicKey))
                PlayerPrefs.SetString(PrefKeyPublicKey, PlayerPrefs.GetString(legacyPk));

            if (PlayerPrefs.HasKey(legacyAuthToken) && !PlayerPrefs.HasKey(PrefKeyAuthToken))
                PlayerPrefs.SetString(PrefKeyAuthToken, PlayerPrefs.GetString(legacyAuthToken));

            PlayerPrefs.DeleteKey(legacyPk);
            PlayerPrefs.DeleteKey(legacyAuthToken);
            PlayerPrefs.Save();
        }

        protected override async Task<Account> _Login(string password = null)
        {
            if (_walletOptions.keepConnectionAlive)
            {
                string pk = PlayerPrefs.GetString(PrefKeyPublicKey, null);
                string authToken = await _authCache.Get();
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
                            await _authCache.Set(_authToken);
                            var resolvedKey = !string.IsNullOrEmpty(reauthPublicKey) ? reauthPublicKey : pk;
                            return new Account(string.Empty, new PublicKey(resolvedKey));
                        }
                    }
                    // Reauthorize failed or returned empty token - clear cached credentials.
                    // Drop _authToken too; the reauthorize lambda may have populated it from
                    // a stale wallet response, and leaving it set would push the next call
                    // down the Reauthorize() branch with a token we already know is bad.
                    _authToken = null;
                    PlayerPrefs.DeleteKey(PrefKeyPublicKey);
                    PlayerPrefs.Save();
                    await _authCache.Clear();
                }
                else if (!pk.IsNullOrEmpty())
                {
                    // Inconsistent state: pk persisted but no auth token. Wipe memory too
                    // so a re-entrant Login on the same instance cannot reuse a stale token.
                    _authToken = null;
                    PlayerPrefs.DeleteKey(PrefKeyPublicKey);
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
            var publicKey = new PublicKey(authorization.PublicKey);
            if (!string.IsNullOrEmpty(authorization.AuthToken))
            {
                _authToken = authorization.AuthToken;
                if (_walletOptions.keepConnectionAlive)
                {
                    PlayerPrefs.SetString(PrefKeyPublicKey, publicKey.ToString());
                    PlayerPrefs.Save();
                    await _authCache.Set(_authToken);
                }
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
                _authToken = await _authCache.Get();

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
            if (!string.IsNullOrEmpty(authorization.AuthToken))
            {
                _authToken = authorization.AuthToken;
                if (_walletOptions.keepConnectionAlive)
                {
                    await _authCache.Set(_authToken);
                }
            }
            return res.SignedPayloads.Select(transaction => Transaction.Deserialize(transaction)).ToArray();
        }


        /// <summary>
        /// Clears the in-memory token, the cached public key in PlayerPrefs,
        /// and the auth token stored in <see cref="IMwaAuthCache"/>. Does
        /// NOT call <c>deauthorize</c> on the wallet side. Use
        /// <see cref="DisconnectWallet"/> when the wallet-side session also
        /// needs to be revoked.
        ///
        /// Stays synchronous to keep the <see cref="WalletBase"/> override
        /// signature stable. The cache <see cref="IMwaAuthCache.Clear"/>
        /// call is awaited synchronously, so custom cache impls must not
        /// block on UI or network here.
        /// </summary>
        public override void Logout()
        {
            base.Logout();
            PlayerPrefs.DeleteKey(PrefKeyPublicKey);
            PlayerPrefs.Save();
            _authToken = null;
            try
            {
                // Custom IMwaAuthCache impls (Keystore, EncryptedSharedPreferences, etc.) can
                // throw on backend errors. Swallow here so DisconnectWallet still fires
                // OnWalletDisconnected and the rest of the logout sequence completes.
                _authCache.Clear().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MWA] Auth cache clear failed during Logout: {e}");
            }
        }

        public async Task DisconnectWallet()
        {
            string authToken = _authToken;
            if (authToken.IsNullOrEmpty())
                authToken = await _authCache.Get();

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
                Debug.LogError($"[MWA] ReconnectWallet failed: {e}");
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
                _authToken = await _authCache.Get();

            string cachedPk = Account?.PublicKey?.ToString()
                ?? PlayerPrefs.GetString(PrefKeyPublicKey, null);
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
            if (!string.IsNullOrEmpty(authorization.AuthToken))
            {
                _authToken = authorization.AuthToken;
                if (_walletOptions.keepConnectionAlive)
                {
                    await _authCache.Set(_authToken);
                }
            }
            return signedMessages.SignedPayloadsBytes[0];
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in phantom wallet");
        }
    }
}
