using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Solana.Unity.SDK;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
[Preserve]
public class MobileWalletAdapterClient : JsonRpc20Client, IAdapterOperations, IMessageReceiver
{
    private int _mNextMessageId = 1;

    public MobileWalletAdapterClient(IMessageSender messageSender) : base(messageSender)
    {
    }

    [Preserve]
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string cluster)
    {
        var request = PrepareAuthRequest(identityUri, iconUri, identityName, cluster, "authorize");
        return SendRequest<AuthorizationResult>(request);
    }

    public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken)
    {
        var request = PrepareAuthRequest(identityUri, iconUri, identityName, null, "reauthorize");
        request.Params.AuthToken = authToken;
        return SendRequest<AuthorizationResult>(request);
    }

    /// <summary>
    /// Revokes the given auth token with the wallet app. The wallet will discard the session.
    /// Always call this before clearing local state on logout.
    /// </summary>
    public Task Deauthorize(string authToken)
    {
        var request = new JsonRequest
        {
            JsonRpc = "2.0",
            Method = "deauthorize",
            Params = new JsonRequest.JsonRequestParams
            {
                AuthToken = authToken
            },
            Id = NextMessageId()
        };
        return SendRequest<object>(request);
    }

    /// <summary>
    /// Queries the connected wallet for its supported capabilities and limits.
    /// Use this to adapt batch sizes and feature detection for your app.
    /// </summary>
    public Task<WalletCapabilities> GetCapabilities()
    {
        var request = new JsonRequest
        {
            JsonRpc = "2.0",
            Method = "get_capabilities",
            Params = new JsonRequest.JsonRequestParams(),
            Id = NextMessageId()
        };
        return SendRequest<WalletCapabilities>(request);
    }

    public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions)
    {
        var request = PrepareSignTransactionsRequest(transactions);
        return SendRequest<SignedResult>(request);
    }

    public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses)
    {
        var request = PrepareSignMessagesRequest(messages, addresses);
        return SendRequest<SignedResult>(request);
    }

    private JsonRequest PrepareAuthRequest(Uri uriIdentity, Uri icon, string name, string cluster, string method)
    {
        if (uriIdentity != null && !uriIdentity.IsAbsoluteUri)
            throw new ArgumentException("If non-null, identityUri must be an absolute, hierarchical Uri");
        if (icon != null && icon.IsAbsoluteUri)
            throw new ArgumentException("If non-null, iconRelativeUri must be a relative Uri");

        return new JsonRequest
        {
            JsonRpc = "2.0",
            Method = method,
            Params = new JsonRequest.JsonRequestParams
            {
                Identity = new JsonRequest.JsonRequestIdentity
                {
                    Uri = uriIdentity,
                    Icon = icon,
                    Name = name
                },
                Cluster = cluster
            },
            Id = NextMessageId()
        };
    }

    private JsonRequest PrepareSignTransactionsRequest(IEnumerable<byte[]> transactions)
    {
        return new JsonRequest
        {
            JsonRpc = "2.0",
            Method = "sign_transactions",
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
            JsonRpc = "2.0",
            Method = "sign_messages",
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