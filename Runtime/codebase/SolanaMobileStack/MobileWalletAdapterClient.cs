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
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string cluster)
    {
        var request = PrepareAuthRequest(
            identityUri,
            iconUri,
            identityName,
            cluster,
            "authorize");

        return SendRequest<AuthorizationResult>(request, "authorize");
    }

    public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken)
    {
        var request = PrepareAuthRequest(
            identityUri,
            iconUri,
            identityName,
            null,
            "reauthorize");

        request.Params.AuthToken = authToken;

        return SendRequest<AuthorizationResult>(request, "reauthorize");
    }

    public Task Deauthorize(string authToken)
    {
        var request = PrepareDeauthorizeRequest(authToken);
        return SendRequest<object>(request, "deauthorize");
    }

    public Task<CapabilitiesResult> GetCapabilities()
    {
        var request = PrepareGetCapabilitiesRequest();
        return SendRequest<CapabilitiesResult>(request, "get_capabilities");
    }

    public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions)
    {
        var request = PrepareSignTransactionsRequest(transactions);
        return SendRequest<SignedResult>(request, "sign_transactions");
    }

    public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses)
    {
        var request = PrepareSignMessagesRequest(messages, addresses);
        return SendRequest<SignedResult>(request, "sign_messages");
    }

    [Preserve]
    public Task<AuthorizationResult> Authorize(
        Uri identityUri, Uri iconUri, string identityName,
        string chain, string[] features, string[] addresses,
        string authToken, JsonRequest.SignInPayload signInPayload)
    {
        if (identityUri != null && !identityUri.IsAbsoluteUri)
        {
            throw new ArgumentException("If non-null, identityUri must be an absolute, hierarchical Uri");
        }
        if (iconUri != null && iconUri.IsAbsoluteUri)
        {
            throw new ArgumentException("If non-null, iconRelativeUri must be a relative Uri");
        }

        var request = new JsonRequest
        {
            JsonRpc = "2.0",
            Method = "authorize",
            Params = new JsonRequest.JsonRequestParams
            {
                Identity = new JsonRequest.JsonRequestIdentity
                {
                    Uri = identityUri,
                    Icon = iconUri,
                    Name = identityName
                },
                Chain = chain,
                Features = features?.ToList(),
                Addresses = addresses?.ToList(),
                AuthToken = authToken,
                SignInPayloadData = signInPayload
            },
            Id = NextMessageId()
        };

        return SendRequest<AuthorizationResult>(request, "authorize");
    }

    public Task<SignAndSendResult> SignAndSendTransactions(IEnumerable<byte[]> transactions, JsonRequest.SignAndSendOptions options)
    {
        var request = new JsonRequest
        {
            JsonRpc = "2.0",
            Method = "sign_and_send_transactions",
            Params = new JsonRequest.JsonRequestParams
            {
                Payloads = transactions.Select(Convert.ToBase64String).ToList(),
                Options = options
            },
            Id = NextMessageId()
        };

        return SendRequest<SignAndSendResult>(request, "sign_and_send_transactions");
    }

    private JsonRequest PrepareAuthRequest(Uri uriIdentity, Uri icon, string name, string cluster, string method)
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
                Cluster = cluster
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