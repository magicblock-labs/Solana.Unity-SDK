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
        
        return SendRequest<AuthorizationResult>(request);
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

        return SendRequest<AuthorizationResult>(request);
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
        {
            throw new ArgumentException("If non-null, identityUri must be an absolute, hierarchical Uri");
        }
        if (icon != null && icon.IsAbsoluteUri)
        {
            throw new ArgumentException("If non-null, iconRelativeUri must be a relative Uri");
        }
        var request = new JsonRequest
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
        return request;
    }
    
    private JsonRequest PrepareSignTransactionsRequest(IEnumerable<byte[]> transactions)
    {
        var request = new JsonRequest
        {
            JsonRpc = "2.0",
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
            JsonRpc = "2.0",
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