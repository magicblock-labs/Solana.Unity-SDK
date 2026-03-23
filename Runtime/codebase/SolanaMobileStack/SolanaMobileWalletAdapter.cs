using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.Events;
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

        /// <summary>
        /// When true, the auth token is cached via <see cref="IMwaAuthCache"/> so users
        /// are not re-prompted on every app launch.
        /// </summary>
        public bool keepConnectionAlive = true;
    }


    [Obsolete("Use SolanaWalletAdapter class instead, which is the cross platform wrapper.")]
    public class SolanaMobileWalletAdapter : WalletBase
    {
        private readonly SolanaMobileWalletAdapterOptions _walletOptions;
        private readonly IMwaAuthCache _authCache;

        private Transaction _currentTransaction;
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private readonly WalletBase _internalWallet;

        /// <summary>Cached auth token — persisted across sessions via <see cref="_authCache"/>.</summary>
        private string _authToken;

        /// <summary>Wallet identity key used for cache lookups (derived from options).</summary>
        private string WalletIdentity => _walletOptions.identityUri + "|" + _walletOptions.name;

        // ─── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired after a successful Deauthorize + state clear (explicit logout).</summary>
        public event Action OnWalletDisconnected;

        /// <summary>Fired after a successful Reauthorize using a cached token (silent reconnect).</summary>
        public event Action OnWalletReconnected;

        // ─── Constructor ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an instance of the Android MWA adapter.
        /// </summary>
        /// <param name="solanaWalletOptions">Wallet identity options.</param>
        /// <param name="authCache">
        /// Optional custom auth cache. Defaults to <see cref="PlayerPrefsAuthCache"/> when null.
        /// Inject a custom <see cref="IMwaAuthCache"/> implementation for encrypted storage.
        /// </param>
        public SolanaMobileWalletAdapter(
            SolanaMobileWalletAdapterOptions solanaWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null,
            string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false,
            IMwaAuthCache authCache = null
        ) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            _walletOptions = solanaWalletOptions;
            _authCache = authCache ?? new PlayerPrefsAuthCache();

            if (Application.platform != RuntimePlatform.Android)
                throw new Exception("SolanaMobileWalletAdapter can only be used on Android");
        }

        // ─── Login ──────────────────────────────────────────────────────────────

        protected override async Task<Account> _Login(string password = null)
        {
            var cluster = RPCNameMap[(int)RpcCluster];

            // Step 1: Try to reauthorize silently using cached token
            if (_walletOptions.keepConnectionAlive)
            {
                var cachedToken = await _authCache.GetAuthToken(WalletIdentity);
                if (!string.IsNullOrEmpty(cachedToken))
                {
                    var reauthorizeResult = await TryReauthorize(cachedToken, cluster);
                    if (reauthorizeResult != null)
                    {
                        _authToken = reauthorizeResult.AuthToken;
                        await _authCache.SetAuthToken(WalletIdentity, _authToken);
                        var cachedPublicKey = new PublicKey(reauthorizeResult.PublicKey);
                        OnWalletReconnected?.Invoke();
                        return new Account(string.Empty, cachedPublicKey);
                    }
                    // Cached token was invalid — clear it and fall through to full authorize
                    await _authCache.ClearAuthToken(WalletIdentity);
                }
            }

            // Step 2: Full authorization (prompts user in wallet app)
            AuthorizationResult authorization = null;
            var localAssociationScenario = new LocalAssociationScenario();
            var result = await localAssociationScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        authorization = await client.Authorize(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name,
                            cluster);
                    }
                }
            );

            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }

            _authToken = authorization.AuthToken;

            // Persist the new token
            if (_walletOptions.keepConnectionAlive)
                await _authCache.SetAuthToken(WalletIdentity, _authToken);

            return new Account(string.Empty, new PublicKey(authorization.PublicKey));
        }

        // ─── Disconnect / Reconnect ──────────────────────────────────────────────

        /// <summary>
        /// Explicitly disconnects the wallet by sending a Deauthorize request to the wallet app,
        /// then clears all cached state. Fires <see cref="OnWalletDisconnected"/>.
        /// 
        /// Use this for a "Sign Out" button in your game UI.
        /// </summary>
        public async Task DisconnectWallet()
        {
            if (!string.IsNullOrEmpty(_authToken))
            {
                try
                {
                    var localAssociationScenario = new LocalAssociationScenario();
                    var currentToken = _authToken; // capture before clearing
                    await localAssociationScenario.StartAndExecute(
                        new List<Action<IAdapterOperations>>
                        {
                            async client => await client.Deauthorize(currentToken)
                        }
                    );
                }
                catch (Exception e)
                {
                    // Best-effort — don't block logout if deauthorize fails (e.g. wallet not installed)
                    Debug.LogWarning($"[MWA] Deauthorize failed during disconnect: {e.Message}");
                }
            }

            // Clear all local auth state
            _authToken = null;
            await _authCache.ClearAuthToken(WalletIdentity);

            // Notify and base logout
            OnWalletDisconnected?.Invoke();
            base.Logout();
        }

        /// <summary>
        /// Attempts a silent reconnect using the cached auth token.
        /// If the token is expired or missing, performs a full Authorize (user is prompted).
        /// Fires <see cref="OnWalletReconnected"/> on silent success.
        /// </summary>
        public async Task<Account> ReconnectWallet()
        {
            return await Login();
        }

        /// <summary>
        /// Queries the wallet for its supported capabilities.
        /// Returns null if the wallet does not support the get_capabilities endpoint.
        /// </summary>
        public async Task<WalletCapabilities> GetCapabilities()
        {
            WalletCapabilities capabilities = null;
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
                Debug.LogWarning($"[MWA] GetCapabilities failed: {result.Error?.Message}");
                return null;
            }

            return capabilities;
        }

        // ─── Sign Transactions ───────────────────────────────────────────────────

        protected override async Task<Transaction> _SignTransaction(Transaction transaction)
        {
            var result = await _SignAllTransactions(new Transaction[] { transaction });
            return result[0];
        }

        protected override async Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
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
                        if (string.IsNullOrEmpty(_authToken))
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
                        res = await client.SignTransactions(
                            transactions.Select(transaction => transaction.Serialize()).ToList());
                    }
                }
            );

            if (!result.WasSuccessful)
            {
                Debug.LogError(result.Error.Message);
                throw new Exception(result.Error.Message);
            }

            _authToken = authorization.AuthToken;

            // Keep the cache up-to-date with the latest auth token
            if (_walletOptions.keepConnectionAlive)
                await _authCache.SetAuthToken(WalletIdentity, _authToken);

            return res.SignedPayloads.Select(transaction => Transaction.Deserialize(transaction)).ToArray();
        }

        // ─── Sign Messages ───────────────────────────────────────────────────────

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
                        if (string.IsNullOrEmpty(_authToken))
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

            if (_walletOptions.keepConnectionAlive)
                await _authCache.SetAuthToken(WalletIdentity, _authToken);

            return signedMessages.SignedPayloadsBytes[0];
        }

        // ─── Logout ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Performs a full cleanup: deauthorizes the token with the wallet app, clears all 
        /// cached state, and fires <see cref="OnWalletDisconnected"/>.
        /// Equivalent to <see cref="DisconnectWallet"/> — prefer that method for UI-triggered logouts.
        /// </summary>
        public override void Logout()
        {
            // Fire-and-forget the async disconnect (can't await in override void)
            _ = DisconnectWallet();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts a silent reauthorize with the given token.
        /// Returns null if the token is expired or invalid.
        /// </summary>
        private async Task<AuthorizationResult> TryReauthorize(string cachedToken, string cluster)
        {
            try
            {
                AuthorizationResult result = null;
                var scenario = new LocalAssociationScenario();
                var response = await scenario.StartAndExecute(
                    new List<Action<IAdapterOperations>>
                    {
                        async client =>
                        {
                            result = await client.Reauthorize(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name,
                                cachedToken);
                        }
                    }
                );

                return response.WasSuccessful ? result : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MWA] Silent reauthorize failed: {e.Message}");
                return null;
            }
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in a MWA wallet");
        }
    }
}
