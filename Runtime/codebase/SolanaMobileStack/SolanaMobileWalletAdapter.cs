using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SolanaMobileStack;
using Solana.Unity.Wallet;
using UnityEngine;

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
        public IAuthorizationCache Cache;
        public LogVerbosity Verbosity = LogVerbosity.Default;
    }


    [Obsolete("Use SolanaWalletAdapter class instead, which is the cross platform wrapper.")]
    public class SolanaMobileWalletAdapter : WalletBase
    {
        public const int ExpectedSchemaVersion = 2;

        private readonly SolanaMobileWalletAdapterOptions _walletOptions;
        private readonly IAuthorizationCache _cache;
        private readonly LogVerbosity _verbosity;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string _currentOperation;

        private Transaction _currentTransaction;

        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private readonly WalletBase _internalWallet;
        private string _authToken;

        public event Action OnWalletDisconnected;
        public event Action OnWalletReconnected;

        private static string ToChainUri(RpcCluster cluster) => cluster switch
        {
            RpcCluster.MainNet => "solana:mainnet",
            RpcCluster.DevNet  => "solana:devnet",
            RpcCluster.TestNet => "solana:testnet",
            _                  => "solana:devnet",
        };

        private static string ToClusterName(RpcCluster cluster) => cluster switch
        {
            RpcCluster.MainNet => "mainnet-beta",
            RpcCluster.DevNet  => "devnet",
            RpcCluster.TestNet => "testnet",
            _                  => "devnet",
        };

        public SolanaMobileWalletAdapter(
            SolanaMobileWalletAdapterOptions solanaWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null,
            string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup
        )
        {
            _walletOptions = solanaWalletOptions;
            _cache = solanaWalletOptions?.Cache ?? new PlayerPrefsAuthorizationCache();
            _verbosity = solanaWalletOptions?.Verbosity ?? LogVerbosity.Default;
            if (Application.platform != RuntimePlatform.Android)
            {
                throw new Exception("SolanaMobileWalletAdapter can only be used on Android");
            }
            MigrateLegacyPrefKeys();
        }

        private void MigrateLegacyPrefKeys()
        {
            const string legacyPk = "pk";
            const string legacyAuthToken = "authToken";
            const string pr269Pk = "solana_sdk.mwa.public_key";
            const string pr269AuthToken = "solana_sdk.mwa.auth_token";

            string pk = null;
            string token = null;

            if (PlayerPrefs.HasKey(pr269Pk) || PlayerPrefs.HasKey(pr269AuthToken))
            {
                pk = PlayerPrefs.GetString(pr269Pk, null);
                token = PlayerPrefs.GetString(pr269AuthToken, null);
                PlayerPrefs.DeleteKey(pr269Pk);
                PlayerPrefs.DeleteKey(pr269AuthToken);
            }
            else if (PlayerPrefs.HasKey(legacyPk) || PlayerPrefs.HasKey(legacyAuthToken))
            {
                pk = PlayerPrefs.GetString(legacyPk, null);
                token = PlayerPrefs.GetString(legacyAuthToken, null);
                PlayerPrefs.DeleteKey(legacyPk);
                PlayerPrefs.DeleteKey(legacyAuthToken);
            }
            else
            {
                return;
            }

            if (!string.IsNullOrEmpty(pk) && !string.IsNullOrEmpty(token))
            {
                _cache.SetAsync(new AuthorizationRecord
                {
                    SchemaVersion       = ExpectedSchemaVersion,
                    AuthToken           = token,
                    AccountAddress      = pk,
                    CachedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                }).GetAwaiter().GetResult();
            }

            PlayerPrefs.Save();
        }

        private async Task<AuthorizationRecord> LoadValidCachedRecordAsync()
        {
            var record = await _cache.GetAsync();
            if (record == null)
                return null;
            if (record.SchemaVersion != ExpectedSchemaVersion)
            {
                await _cache.ClearAsync();
                return null;
            }
            var currentChain = ToChainUri(RpcCluster);
            if (record.Chain != null && record.Chain != currentChain)
            {
                await _cache.ClearAsync();
                return null;
            }
            return record;
        }

        private void CacheAuthorization(AuthorizationResult authorization)
        {
            if (_walletOptions?.keepConnectionAlive ?? true)
            {
                _cache.SetAsync(new AuthorizationRecord
                {
                    SchemaVersion       = ExpectedSchemaVersion,
                    Chain               = authorization.PrimaryAccount().Chains?.FirstOrDefault() ?? ToChainUri(RpcCluster),
                    AuthToken           = authorization.AuthToken,
                    AccountAddress      = authorization.PrimaryAccount().Address,
                    AccountLabel        = authorization.PrimaryAccount().Label,
                    AccountIcon         = authorization.PrimaryAccount().Icon,
                    Chains              = authorization.PrimaryAccount().Chains,
                    Features            = authorization.PrimaryAccount().Features,
                    WalletUriBase       = authorization.WalletUriBase,
                    WalletIcon          = authorization.WalletIcon,
                    CachedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                });
            }
        }

        private async Task ReloadAuthTokenFromCacheIfNeeded()
        {
            if (!string.IsNullOrEmpty(_authToken)) return;
            if (!(_walletOptions?.keepConnectionAlive ?? true)) return;
            var record = await _cache.GetAsync();
            if (record != null && !string.IsNullOrEmpty(record.AuthToken))
                _authToken = record.AuthToken;
        }

        protected override async Task<Account> _Login(string password = null)
        {
            var chain = ToChainUri(RpcCluster);

            var reconnect = await ReconnectInternal();
            if (reconnect is ReconnectResult.SilentSuccess success)
                return success.Account;

            AuthorizationResult authorization = null;
            var scenario = new LocalAssociationScenario();
            var result = await scenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        authorization = await client.AuthorizeAsync(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, chain, null, CancellationToken.None);
                    }
                }
            );
            if (!result.WasSuccessful)
                throw new Exception(result.Error?.Message ?? "Authorization failed");
            if (authorization == null)
                throw new Exception("Authorization was not populated by wallet");

            _authToken = authorization.AuthToken;
            CacheAuthorization(authorization);
            var publicKey = new PublicKey(authorization.PrimaryAccountPublicKeyBytes());
            return new Account(string.Empty, publicKey);
        }

        protected override async Task<Transaction> _SignTransaction(Transaction transaction)
        {
            var result = await _SignAllTransactions(new Transaction[] { transaction });
            return result[0];
        }

        protected override async Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            await ReloadAuthTokenFromCacheIfNeeded();
            var chain = ToChainUri(RpcCluster);
            SignedResult res = null;
            AuthorizationResult authorization = null;
            var scenario = new LocalAssociationScenario();
            var result = await scenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        authorization = await client.AuthorizeAsync(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, chain, _authToken, CancellationToken.None);
                    },
                    async client =>
                    {
                        res = await client.SignTransactions(
                            transactions.Select(transaction => transaction.Serialize()).ToList());
                    }
                }
            );
            if (!result.WasSuccessful)
                throw new Exception(result.Error?.Message ?? "Sign transactions failed");
            if (authorization == null)
                throw new Exception("Authorization was not populated by wallet");
            if (res == null)
                throw new Exception("Signed payloads were not populated by wallet");

            _authToken = authorization.AuthToken ?? _authToken;
            CacheAuthorization(authorization);
            return res.SignedPayloads.Select(transaction => Transaction.Deserialize(transaction)).ToArray();
        }

        public async Task<DeauthorizeResult> Deauthorize()
        {
            if (!await TryAcquireGate("Deauthorize"))
                throw new OperationInFlightException($"{_currentOperation} is in flight; cannot start Deauthorize");
            try { return await DeauthorizeInternal(); }
            finally { ReleaseGate(); }
        }

        private async Task<DeauthorizeResult> DeauthorizeInternal()
        {
            var record = await _cache.GetAsync();
            if (record == null)
            {
                _authToken = null;
#pragma warning disable CS0618
                Logout();
#pragma warning restore CS0618
                OnWalletDisconnected?.Invoke();
                return new DeauthorizeResult.FullyRevoked();
            }

            bool rpcSucceeded = false;
            try
            {
                var scenario = new LocalAssociationScenario();
                var result = await scenario.StartAndExecute(
                    new List<Action<IAdapterOperations>>
                    {
                        async client =>
                        {
                            await client.DeauthorizeAsync(record.AuthToken, CancellationToken.None);
                        }
                    }
                );
                rpcSucceeded = result.WasSuccessful;
            }
            catch (Exception)
            {
                // Transport/RPC failure — proceed to cache clear (LocalOnly path)
            }

            try
            {
                await _cache.ClearAsync();
            }
            catch (Exception clearEx)
            {
                if (!rpcSucceeded)
                    return new DeauthorizeResult.Failed { Error = clearEx };
            }

            _authToken = null;
#pragma warning disable CS0618
            Logout();
#pragma warning restore CS0618
            OnWalletDisconnected?.Invoke();

            if (rpcSucceeded)
                return new DeauthorizeResult.FullyRevoked();

            return new DeauthorizeResult.LocalOnly { WalletPackage = record.WalletUriBase };
        }

        private async Task<bool> TryAcquireGate(string operationName)
        {
            if (!await _gate.WaitAsync(0))
                return false;
            _currentOperation = operationName;
            return true;
        }

        private void ReleaseGate()
        {
            _currentOperation = null;
            _gate.Release();
        }

        public async Task<SignAndSendTxResult> SignAndSendTransactions(
            Transaction[] transactions, SendOptions options = null)
        {
            if (!await TryAcquireGate("SignAndSendTransactions"))
                throw new OperationInFlightException(
                    $"{_currentOperation} is in flight; cannot start SignAndSendTransactions");
            try { return await SignAndSendTransactionsInternal(transactions, options); }
            finally { ReleaseGate(); }
        }

        public override async Task<RequestResult<string>> SignAndSendTransaction(
            Transaction transaction, bool skipPreflight = false,
            Commitment commitment = Commitment.Confirmed)
        {
            if (!await TryAcquireGate("SignAndSendTransaction"))
                throw new OperationInFlightException(
                    $"{_currentOperation} is in flight; cannot start SignAndSendTransaction");
            try
            {
                var opts = new SendOptions
                {
                    SkipPreflight = skipPreflight,
                    Commitment = commitment
                };
                var result = await SignAndSendTransactionsInternal(
                    new[] { transaction }, opts);

                if (result is SignAndSendTxResult.Success success && success.Signatures.Length > 0)
                {
                    var sig = Convert.ToBase64String(success.Signatures[0]);
                    return new RequestResult<string> { Result = sig, WasRequestSuccessfullyHandled = true };
                }

                return new RequestResult<string>
                {
                    Result = null,
                    WasRequestSuccessfullyHandled = false,
                    Reason = result.ToString()
                };
            }
            finally { ReleaseGate(); }
        }

        private async Task<SignAndSendTxResult> SignAndSendTransactionsInternal(
            Transaction[] transactions, SendOptions options)
        {
            await ReloadAuthTokenFromCacheIfNeeded();

            if (options != null && options.MinContextSlot == null)
            {
                try
                {
                    var blockHash = await ActiveRpcClient.GetLatestBlockHashAsync(
                        options.Commitment ?? Rpc.Types.Commitment.Confirmed);
                    if (blockHash?.Result?.Context?.Slot != null)
                        options = new SendOptions
                        {
                            Commitment = options.Commitment,
                            SkipPreflight = options.SkipPreflight,
                            MaxRetries = options.MaxRetries,
                            MinContextSlot = blockHash.Result.Context.Slot,
                        };
                }
                catch (Exception) { }
            }

            var payloads = new string[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
                payloads[i] = Convert.ToBase64String(transactions[i].Serialize());

            try
            {
                Newtonsoft.Json.Linq.JToken raw = null;
                var chain = ToChainUri(RpcCluster);
                var scenario = new LocalAssociationScenario();
                var scenarioResult = await scenario.StartAndExecute(
                    new List<Action<IAdapterOperations>>
                    {
                        async client =>
                        {
                            await client.AuthorizeAsync(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name, chain, _authToken, CancellationToken.None);
                        },
                        async client =>
                        {
                            raw = await client.SignAndSendTransactionsAsync(
                                payloads, options, CancellationToken.None);
                        }
                    }
                );

                if (!scenarioResult.WasSuccessful)
                    return new SignAndSendTxResult.WalletUnreachable();

                if (raw == null)
                    return new SignAndSendTxResult.WalletUnreachable();

                var signaturesToken = raw["signatures"];
                if (signaturesToken == null)
                    return new SignAndSendTxResult.WalletUnreachable();

                var sigs = new byte[signaturesToken.Count()][];
                for (int i = 0; i < sigs.Length; i++)
                    sigs[i] = Convert.FromBase64String((string)signaturesToken[i]);

                return new SignAndSendTxResult.Success { Signatures = sigs };
            }
            catch (JsonRpcException jrpc)
            {
                switch (jrpc.Code)
                {
                    case -1:
                        try { await _cache.ClearAsync(); } catch { }
                        _authToken = null;
                        return new SignAndSendTxResult.AuthRevoked();
                    case -2:
                        bool[] valid = null;
                        try { var d = jrpc.Data; if (d?["valid"] != null) valid = d["valid"].ToObject<bool[]>(); } catch { }
                        return new SignAndSendTxResult.InvalidPayloads { Valid = valid };
                    case -3:
                        return new SignAndSendTxResult.UserDenied();
                    case -4:
                        byte[][] partialSigs = null;
                        try
                        {
                            var d = jrpc.Data; var sa = d?["signatures"];
                            if (sa != null) { partialSigs = new byte[sa.Count()][]; for (int i = 0; i < partialSigs.Length; i++) { var s = (string)sa[i]; partialSigs[i] = s != null ? Convert.FromBase64String(s) : null; } }
                        } catch { }
                        return new SignAndSendTxResult.NotSubmitted { PartialSignatures = partialSigs };
                    case -6:
                        uint? max = null;
                        try { var d = jrpc.Data; if (d?["max_transactions_per_request"] != null) max = d["max_transactions_per_request"].ToObject<uint>(); } catch { }
                        return new SignAndSendTxResult.TooManyPayloads { MaxTransactionsPerRequest = max };
                    case -7:
                        return new SignAndSendTxResult.ChainNotSupported();
                    default:
                        return new SignAndSendTxResult.WalletUnreachable();
                }
            }
            catch (Exception)
            {
                return new SignAndSendTxResult.WalletUnreachable();
            }
        }

        public async Task Disconnect()
        {
            if (!await TryAcquireGate("Disconnect"))
                throw new OperationInFlightException(
                    $"{_currentOperation} is in flight; cannot start Disconnect");
            try { await DisconnectInternal(); }
            finally { ReleaseGate(); }
        }

        private async Task DisconnectInternal()
        {
            try
            {
                await _cache.ClearAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MWA] Auth cache clear failed during Disconnect: {e}");
            }
            _authToken = null;
#pragma warning disable CS0618
            Logout();
#pragma warning restore CS0618
            OnWalletDisconnected?.Invoke();
        }

        public async Task<ReconnectResult> Reconnect()
        {
            if (!await TryAcquireGate("Reconnect"))
                throw new OperationInFlightException(
                    $"{_currentOperation} is in flight; cannot start Reconnect");
            try { return await ReconnectInternal(); }
            finally { ReleaseGate(); }
        }

        private async Task<ReconnectResult> ReconnectInternal()
        {
            AuthorizationRecord record;
            try
            {
                record = await LoadValidCachedRecordAsync();
            }
            catch (Exception)
            {
                return new ReconnectResult.NoCachedSession();
            }

            if (record == null)
                return new ReconnectResult.NoCachedSession();

            try
            {
                AuthorizationResult authorization = null;
                var chain = ToChainUri(RpcCluster);
                var scenario = new LocalAssociationScenario();
                var result = await scenario.StartAndExecute(
                    new List<Action<IAdapterOperations>>
                    {
                        async client =>
                        {
                            authorization = await client.AuthorizeAsync(
                                new Uri(_walletOptions.identityUri),
                                new Uri(_walletOptions.iconUri, UriKind.Relative),
                                _walletOptions.name, chain, record.AuthToken, CancellationToken.None);
                        }
                    }
                );

                if (!result.WasSuccessful)
                    return new ReconnectResult.Failed
                    {
                        Error = new Exception(result.Error?.Message ?? "Wallet unreachable")
                    };

                _authToken = authorization.AuthToken;
                CacheAuthorization(authorization);

                var publicKey = new PublicKey(authorization.PrimaryAccountPublicKeyBytes());
                var account = new Account(string.Empty, publicKey);
                Account = account;
                OnWalletReconnected?.Invoke();
                return new ReconnectResult.SilentSuccess { Account = account };
            }
            catch (JsonRpcException jrpc) when (jrpc.Code == -1)
            {
                try { await _cache.ClearAsync(); } catch { }
                _authToken = null;
                return new ReconnectResult.NoCachedSession();
            }
            catch (Exception ex)
            {
                return new ReconnectResult.Failed { Error = ex };
            }
        }

        [System.Obsolete("Use Disconnect() for local clear or Deauthorize() to revoke wallet-side. See docs/migration-v1-to-v2.md.")]
        public override void Logout()
        {
            base.Logout();
        }

        public async Task DisconnectWallet()
        {
            await Disconnect();
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
                throw new Exception(result.Error?.Message ?? "GetCapabilities failed");
            if (capabilities == null)
                throw new Exception("GetCapabilities RPC succeeded but returned no data");
            return capabilities;
        }

        public async Task<(Account Account, SignInResult SignInResult)> LoginWithSignIn(
            SignInPayload signInPayload)
        {
            if (!await TryAcquireGate("LoginWithSignIn"))
                throw new OperationInFlightException(
                    $"{_currentOperation} is in flight; cannot start LoginWithSignIn");
            try { return await LoginWithSignInInternal(signInPayload); }
            finally { ReleaseGate(); }
        }

        private async Task<(Account Account, SignInResult SignInResult)> LoginWithSignInInternal(
            SignInPayload signInPayload)
        {
            var chain = ToChainUri(RpcCluster);

            AuthorizationResult authorization = null;

            var scenario = new LocalAssociationScenario();
            var result = await scenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        authorization = await client.AuthorizeAsync(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, chain, null,
                            null, null, signInPayload,
                            CancellationToken.None);
                    }
                }
            );
            if (!result.WasSuccessful)
                throw new Exception(result.Error?.Message ?? "Authorization with sign-in failed");
            if (authorization == null)
                throw new Exception("Authorization was not populated by wallet");

            _authToken = authorization.AuthToken;
            CacheAuthorization(authorization);
            var publicKey = new PublicKey(authorization.PrimaryAccountPublicKeyBytes());
            var account = new Account(string.Empty, publicKey);

            if (authorization.SignInResult != null)
                return (account, authorization.SignInResult);

            SignedResult siwsFallbackSig = null;
            var fallbackScenario = new LocalAssociationScenario();
            var fallbackResult = await fallbackScenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        await client.AuthorizeAsync(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, chain, _authToken,
                            CancellationToken.None);
                    },
                    async client =>
                    {
                        var pubkeyBase58 = publicKey.Key;
                        var siwsMessage = $"{signInPayload.Domain} wants you to sign in with your Solana account:\n{pubkeyBase58}";
                        if (!string.IsNullOrEmpty(signInPayload.Statement))
                            siwsMessage += $"\n\n{signInPayload.Statement}";
                        if (!string.IsNullOrEmpty(signInPayload.Uri))
                            siwsMessage += $"\n\nURI: {signInPayload.Uri}";

                        var messageBytes = System.Text.Encoding.UTF8.GetBytes(siwsMessage);
                        siwsFallbackSig = await client.SignMessages(
                            messages: new List<byte[]> { messageBytes },
                            addresses: new List<byte[]> { authorization.PrimaryAccountPublicKeyBytes() });
                    }
                }
            );
            if (!fallbackResult.WasSuccessful)
                throw new Exception(fallbackResult.Error?.Message ?? "SIWS fallback signing failed");

            if (siwsFallbackSig?.SignedPayloadsBytes?.Count > 0)
            {
                var signedBytes = siwsFallbackSig.SignedPayloadsBytes[0];
                var sigBytes = new byte[64];
                var msgBytes = new byte[signedBytes.Length - 64];
                System.Array.Copy(signedBytes, 0, msgBytes, 0, msgBytes.Length);
                System.Array.Copy(signedBytes, signedBytes.Length - 64, sigBytes, 0, 64);

                return (account, new SignInResult
                {
                    Address = authorization.PrimaryAccount().Address,
                    SignedMessage = Convert.ToBase64String(msgBytes),
                    Signature = Convert.ToBase64String(sigBytes),
                    SignatureType = "ed25519"
                });
            }

            throw new Exception("SIWS failed: wallet did not return sign_in_result and fallback signing failed");
        }

        public async Task<string> CloneAuthorization()
        {
            if (!await TryAcquireGate("CloneAuthorization"))
                throw new OperationInFlightException(
                    $"{_currentOperation} is in flight; cannot start CloneAuthorization");
            try { return await CloneAuthorizationInternal(); }
            finally { ReleaseGate(); }
        }

        private async Task<string> CloneAuthorizationInternal()
        {
            await ReloadAuthTokenFromCacheIfNeeded();
            if (string.IsNullOrEmpty(_authToken))
                throw new InvalidOperationException("No auth token available — connect first");

            string clonedToken = null;
            var chain = ToChainUri(RpcCluster);
            var scenario = new LocalAssociationScenario();
            var result = await scenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        await client.AuthorizeAsync(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, chain, _authToken, CancellationToken.None);
                    },
                    async client =>
                    {
                        clonedToken = await client.CloneAuthorizationAsync(
                            _authToken, CancellationToken.None);
                    }
                }
            );
            if (!result.WasSuccessful)
                throw new Exception(result.Error?.Message ?? "Clone authorization failed");
            if (string.IsNullOrEmpty(clonedToken))
                throw new Exception("Clone authorization RPC succeeded but returned no token");
            return clonedToken;
        }

        public override async Task<byte[]> SignMessage(byte[] message)
        {
            await ReloadAuthTokenFromCacheIfNeeded();

            string cachedPk = Account?.PublicKey?.ToString();
            if (string.IsNullOrEmpty(cachedPk))
            {
                var record = await _cache.GetAsync();
                cachedPk = record?.AccountAddress;
            }
            if (string.IsNullOrEmpty(cachedPk))
                throw new Exception("Cannot sign message: no account available");

            SignedResult signedMessages = null;
            AuthorizationResult authorization = null;
            var chain = ToChainUri(RpcCluster);
            var scenario = new LocalAssociationScenario();
            var result = await scenario.StartAndExecute(
                new List<Action<IAdapterOperations>>
                {
                    async client =>
                    {
                        authorization = await client.AuthorizeAsync(
                            new Uri(_walletOptions.identityUri),
                            new Uri(_walletOptions.iconUri, UriKind.Relative),
                            _walletOptions.name, chain, _authToken, CancellationToken.None);
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
                throw new Exception(result.Error?.Message ?? "Sign message failed");
            if (authorization == null)
                throw new Exception("Authorization was not populated by wallet");
            if (signedMessages == null)
                throw new Exception("Signed payloads were not populated by wallet");

            _authToken = authorization.AuthToken ?? _authToken;
            CacheAuthorization(authorization);
            return signedMessages.SignedPayloadsBytes[0];
        }

        public Task<byte[]> SignMessage(string message)
        {
            return SignMessage(System.Text.Encoding.UTF8.GetBytes(message));
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException("Can't create a new account in phantom wallet");
        }
    }
}
