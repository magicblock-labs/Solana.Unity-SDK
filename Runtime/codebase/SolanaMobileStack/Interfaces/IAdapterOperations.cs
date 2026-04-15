using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.SDK;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public interface IAdapterOperations
{
    [Preserve]
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string rpcCluster);
    [Preserve]
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName,
        string chain, string[] features, string[] addresses, string authToken,
        JsonRequest.SignInPayload signInPayload);
    [Preserve]
    public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken);
    [Preserve]
    public Task Deauthorize(string authToken);
    [Preserve]
    public Task<CapabilitiesResult> GetCapabilities();
    [Preserve]
    public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions);
    [Preserve]
    public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses);
    [Preserve]
    public Task<SignAndSendResult> SignAndSendTransactions(IEnumerable<byte[]> transactions, JsonRequest.SignAndSendOptions options);
}