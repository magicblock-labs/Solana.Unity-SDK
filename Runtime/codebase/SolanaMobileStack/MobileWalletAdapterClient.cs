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
public class MobileWalletAdapterClient: JsonRpc20Client, IAdapterOperations, IMessageReceiver
{
    
    private const string JsonRpcVersion = "2.0";
    
    private int _mNextMessageId = 1;

    public MobileWalletAdapterClient(IMessageSender messageSender) : base(messageSender)
    {
    }
    
    [Preserve]
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string cluster, string chain = null)
    {
        var request = PrepareAuthRequest(
            identityUri,
            iconUri,
            identityName,
            cluster,
            "authorize",
            chain);

        return SendRequest<AuthorizationResult>(request);
    }

    public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken, string cluster = null, string chain = null)
    {
        // MWA 2.0 deprecated the standalone `reauthorize` method in favour of `authorize`
        // carrying an `auth_token`. The `chain` MUST be re-sent here: when it is absent the
        // wallet (e.g. Seeker Seed Vault) defaults the re-established session to
        // solana:mainnet, producing a "Network mismatch" at sign time even though the
        // original authorize was devnet. When the chain matches the token's binding the
        // wallet silently reuses the existing auth_token (no extra user prompt).
        var request = PrepareAuthRequest(
            identityUri,
            iconUri,
            identityName,
            cluster,
            "authorize",
            chain);

        request.Params.AuthToken = authToken;

        return SendRequest<AuthorizationResult>(request);
    }

    public Task Deauthorize(string authToken)
    {
        var request = PrepareDeauthorizeRequest(authToken);
        return SendRequest<object>(request);
    }

    public Task<CapabilitiesResult> GetCapabilities()
    {
        var request = PrepareGetCapabilitiesRequest();
        return SendRequest<CapabilitiesResult>(request);
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

    private JsonRequest PrepareAuthRequest(Uri uriIdentity, Uri icon, string name, string cluster, string method, string chain = null)
    {
        if (uriIdentity != null && !uriIdentity.IsAbsoluteUri)
        {
            throw new ArgumentException("If non-null, identityUri must be an absolute, hierarchical Uri");
        }
        if (icon != null && icon.IsAbsoluteUri)
        {
            throw new ArgumentException("If non-null, iconRelativeUri must be a relative Uri");
        }
        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = method,
            Params = new JsonRequest.JsonRequestParams
            {
                Identity = new JsonRequest.JsonRequestIdentity
                {
                    Uri = uriIdentity,
                    Icon = icon,
                    Name = name
                },
                Cluster = cluster,
                Chain = chain
            },
            Id = NextMessageId()
        };
        return request;
    }

    private JsonRequest PrepareDeauthorizeRequest(string authToken)
    {
        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = "deauthorize",
            Params = new JsonRequest.JsonRequestParams
            {
                AuthToken = authToken
            },
            Id = NextMessageId()
        };
        return request;
    }

    private JsonRequest PrepareGetCapabilitiesRequest()
    {
        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = "get_capabilities",
            Params = new JsonRequest.JsonRequestParams(),
            Id = NextMessageId()
        };
        return request;
    }
    
    private JsonRequest PrepareSignTransactionsRequest(IEnumerable<byte[]> transactions)
    {
        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = "sign_transactions",
            Params = new JsonRequest.JsonRequestParams
            {
                Payloads = transactions.Select(Convert.ToBase64String).ToList()
            },
            Id = NextMessageId()
        };
        return request;
    }
    
    private JsonRequest PrepareSignMessagesRequest(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses)
    {
        var request = new JsonRequest
        {
            JsonRpc = JsonRpcVersion,
            Method = "sign_messages",
            Params = new JsonRequest.JsonRequestParams
            {
                Payloads = messages.Select(Convert.ToBase64String).ToList(),
                Addresses = addresses.Select(Convert.ToBase64String).ToList()
            },
            Id = NextMessageId()
        };
        return request;
    }

    private int NextMessageId()
    {
        return _mNextMessageId++;
    }

}