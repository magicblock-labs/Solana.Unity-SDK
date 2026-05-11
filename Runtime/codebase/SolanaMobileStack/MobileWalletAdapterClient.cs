using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Solana.Unity.SDK;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
[Preserve]
public class MobileWalletAdapterClient: JsonRpc20Client, IAdapterOperations, IMessageReceiver
{
    private const string JsonRpcVersion = "2.0";

    private int _mNextMessageId = 1;

    private readonly LogVerbosity _verbosity;

    private static readonly Dictionary<string, string> ChainClusterMap = new()
    {
        ["solana:mainnet"] = "mainnet-beta",
        ["solana:devnet"] = "devnet",
        ["solana:testnet"] = "testnet",
    };

    private static string ChainToCluster(string chain)
    {
        if (chain != null && ChainClusterMap.TryGetValue(chain, out var cluster))
            return cluster;
        return null;
    }

    public MobileWalletAdapterClient(IMessageSender messageSender)
        : this(messageSender, LogVerbosity.Default)
    {
    }

    public MobileWalletAdapterClient(IMessageSender messageSender, LogVerbosity verbosity) : base(messageSender)
    {
        _verbosity = verbosity;
    }

    [Preserve]
    public Task<AuthorizationResult> AuthorizeAsync(
        Uri identityUri,
        Uri iconUri,
        string identityName,
        string chain,
        string authToken,
        CancellationToken ct)
    {
        return AuthorizeAsync(identityUri, iconUri, identityName, chain, authToken, null, null, null, ct);
    }

    [Preserve]
    public async Task<AuthorizationResult> AuthorizeAsync(
        Uri identityUri,
        Uri iconUri,
        string identityName,
        string chain,
        string authToken,
        string[] features,
        byte[][] addresses,
        Solana.Unity.SolanaMobileStack.SignInPayload signInPayload,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        UnityEngine.Debug.Log($"[MWA Client] authorize: chain={chain}, hasToken={authToken != null}, hasFeatures={features != null}, hasSIWS={signInPayload != null}");

        var request = PrepareAuthRequest(
            identityUri, iconUri, identityName,
            chain, authToken, features, addresses, signInPayload,
            RpcMethodNames.Authorize);

        JToken raw = await SendRequestRaw(request);
        UnityEngine.Debug.Log($"[MWA Client] authorize response received");
        return AuthorizationResponseParser.Parse(raw, _verbosity);
    }

    public Task Deauthorize(string authToken)
    {
        var request = PrepareDeauthorizeRequest(authToken);
        return SendRequest<object>(request);
    }

    [Preserve]
    public async Task DeauthorizeAsync(string authToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(authToken))
            throw new ArgumentNullException(nameof(authToken));
        ct.ThrowIfCancellationRequested();

        var request = PrepareDeauthorizeRequest(authToken);
        await SendRequestRaw(request);
    }

    public Task<CapabilitiesResult> GetCapabilities()
    {
        var request = PrepareGetCapabilitiesRequest();
        return SendRequest<CapabilitiesResult>(request);
    }

    public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions)
    {
        UnityEngine.Debug.Log("[MWA Client] sign_transactions");
        var request = PrepareSignTransactionsRequest(transactions);
        return SendRequest<SignedResult>(request);
    }

    public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses)
    {
        UnityEngine.Debug.Log("[MWA Client] sign_messages");
        var request = PrepareSignMessagesRequest(messages, addresses);
        return SendRequest<SignedResult>(request);
    }

    [Preserve]
    public async Task<JToken> SignAndSendTransactionsAsync(
        string[] base64Payloads,
        Solana.Unity.SolanaMobileStack.SendOptions options,
        CancellationToken ct)
    {
        if (base64Payloads == null || base64Payloads.Length == 0)
            throw new ArgumentException("At least one payload is required", nameof(base64Payloads));
        ct.ThrowIfCancellationRequested();
        UnityEngine.Debug.Log($"[MWA Client] sign_and_send_transactions: {base64Payloads.Length} payload(s)");

        JObject wireOptions = null;
        if (options != null)
        {
            wireOptions = new JObject();
            if (options.Commitment != null)
                wireOptions["commitment"] = options.Commitment.Value.ToString().ToLowerInvariant();
            if (options.SkipPreflight != null)
                wireOptions["skip_preflight"] = options.SkipPreflight.Value;
            if (options.MinContextSlot != null)
                wireOptions["min_context_slot"] = (long)options.MinContextSlot.Value;
            if (options.MaxRetries != null)
                wireOptions["max_retries"] = (long)options.MaxRetries.Value;
            if (options.WaitForCommitmentToSendNextTransaction != null)
                wireOptions["wait_for_commitment_to_send_next_transaction"] =
                    options.WaitForCommitmentToSendNextTransaction.Value;
            if (!wireOptions.HasValues)
                wireOptions = null;
        }

        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = RpcMethodNames.SignAndSendTransactions,
            Params = new JsonRequest.JsonRequestParams
            {
                Payloads = new List<string>(base64Payloads),
                Options = wireOptions
            },
            Id = NextMessageId()
        };

        return await SendRequestRaw(request);
    }

    private JsonRequest PrepareAuthRequest(
        Uri uriIdentity, Uri icon, string name,
        string chain, string authToken,
        string[] features, byte[][] addresses,
        Solana.Unity.SolanaMobileStack.SignInPayload signInPayload,
        string method)
    {
        if (uriIdentity != null && !uriIdentity.IsAbsoluteUri)
            throw new ArgumentException("If non-null, identityUri must be an absolute, hierarchical Uri");
        if (icon != null && icon.IsAbsoluteUri)
            throw new ArgumentException("If non-null, iconRelativeUri must be a relative Uri");

        var requestParams = new JsonRequest.JsonRequestParams
        {
            Identity = new JsonRequest.JsonRequestIdentity
            {
                Uri = uriIdentity,
                Icon = icon,
                Name = name
            },
            Chain = chain,
            Cluster = ChainToCluster(chain),
            AuthToken = authToken
        };

        if (features != null && features.Length > 0)
            requestParams.Features = new List<string>(features);

        if (addresses != null && addresses.Length > 0)
            requestParams.Addresses = addresses.Select(Convert.ToBase64String).ToList();

        if (signInPayload != null)
            requestParams.SignInPayload = signInPayload;

        return new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = method,
            Params = requestParams,
            Id = NextMessageId()
        };
    }

    [Preserve]
    public async Task<string> CloneAuthorizationAsync(string authToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(authToken))
            throw new ArgumentNullException(nameof(authToken));
        ct.ThrowIfCancellationRequested();

        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = RpcMethodNames.CloneAuthorization,
            Params = new JsonRequest.JsonRequestParams
            {
                AuthToken = authToken
            },
            Id = NextMessageId()
        };

        JToken raw = await SendRequestRaw(request);
        return (string)raw?["auth_token"];
    }

    private JsonRequest PrepareDeauthorizeRequest(string authToken)
    {
        return new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = RpcMethodNames.Deauthorize,
            Params = new JsonRequest.JsonRequestParams
            {
                AuthToken = authToken
            },
            Id = NextMessageId()
        };
    }

    private JsonRequest PrepareGetCapabilitiesRequest()
    {
        return new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = RpcMethodNames.GetCapabilities,
            Params = new JsonRequest.JsonRequestParams(),
            Id = NextMessageId()
        };
    }

    private JsonRequest PrepareSignTransactionsRequest(IEnumerable<byte[]> transactions)
    {
        return new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = RpcMethodNames.SignTransactions,
            Params = new JsonRequest.JsonRequestParams
            {
                Payloads = transactions.Select(Convert.ToBase64String).ToList()
            },
            Id = NextMessageId()
        };
    }

    private JsonRequest PrepareSignMessagesRequest(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses)
    {
        return new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = RpcMethodNames.SignMessages,
            Params = new JsonRequest.JsonRequestParams
            {
                Payloads = messages.Select(Convert.ToBase64String).ToList(),
                Addresses = addresses.Select(Convert.ToBase64String).ToList()
            },
            Id = NextMessageId()
        };
    }

    private int NextMessageId()
    {
        return _mNextMessageId++;
    }
}
