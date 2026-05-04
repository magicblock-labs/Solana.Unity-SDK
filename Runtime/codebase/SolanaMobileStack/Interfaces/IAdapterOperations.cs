using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Solana.Unity.SolanaMobileStack;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public interface IAdapterOperations
{
    [Preserve]
    [System.Obsolete("Use AuthorizeAsync with chain parameter instead")]
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string rpcCluster);
    [Preserve]
    [System.Obsolete("Use AuthorizeAsync with authToken parameter instead")]
    public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken);
    [Preserve]
    public Task<AuthorizationResult> AuthorizeAsync(Uri identityUri, Uri iconUri, string identityName, string chain, string authToken, CancellationToken ct);
    [Preserve]
    public Task<AuthorizationResult> AuthorizeAsync(
        Uri identityUri, Uri iconUri, string identityName,
        string chain, string authToken,
        string[] features, byte[][] addresses,
        SignInPayload signInPayload,
        CancellationToken ct);
    [Preserve]
    public Task Deauthorize(string authToken);
    [Preserve]
    public Task<CapabilitiesResult> GetCapabilities();
    [Preserve]
    public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions);
    [Preserve]
    public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses);
    [Preserve]
    public Task DeauthorizeAsync(string authToken, CancellationToken ct);
    [Preserve]
    public Task<JToken> SignAndSendTransactionsAsync(string[] base64Payloads, Solana.Unity.SolanaMobileStack.SendOptions options, CancellationToken ct);
    [Preserve]
    public Task<string> CloneAuthorizationAsync(string authToken, CancellationToken ct);
}