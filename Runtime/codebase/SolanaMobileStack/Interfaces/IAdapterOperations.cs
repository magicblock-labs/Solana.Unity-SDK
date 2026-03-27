using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public interface IAdapterOperations
{
    /// <summary>Requests authorization from the wallet. Returns an auth token on success.</summary>
    [Preserve]
    public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string rpcCluster);

    /// <summary>Re-uses a previously issued auth token to reauthorize without user re-prompt.</summary>
    [Preserve]
    public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken);

    /// <summary>Revokes an auth token so the wallet forgets the session. Always call this before Logout.</summary>
    [Preserve]
    public Task Deauthorize(string authToken);

    /// <summary>Queries the wallet for its supported features and limits.</summary>
    [Preserve]
    public Task<WalletCapabilities> GetCapabilities();

    /// <summary>Requests signing of one or more serialized transactions.</summary>
    [Preserve]
    public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions);

    /// <summary>Requests signing of one or more arbitrary messages.</summary>
    [Preserve]
    public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses);
}