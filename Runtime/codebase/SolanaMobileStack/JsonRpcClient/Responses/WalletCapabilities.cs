using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

/// <summary>
/// Represents the capabilities reported by a connected wallet via the get_capabilities MWA endpoint.
/// All properties are optional — wallets may omit fields they do not support.
/// </summary>
[Preserve]
public class WalletCapabilities
{
    /// <summary>
    /// Maximum number of transaction payloads that can be signed in a single request.
    /// Null if the wallet does not report this limit.
    /// </summary>
    [JsonProperty("max_transactions_per_request")]
    public int? MaxTransactionsPerRequest { get; set; }

    /// <summary>
    /// Maximum number of message payloads that can be signed in a single request.
    /// Null if the wallet does not report this limit.
    /// </summary>
    [JsonProperty("max_messages_per_request")]
    public int? MaxMessagesPerRequest { get; set; }

    /// <summary>
    /// Supported Solana transaction versions (e.g. "legacy", "0").
    /// Null or empty if the wallet does not report this capability.
    /// </summary>
    [JsonProperty("supported_transaction_versions")]
    public List<string> SupportedTransactionVersions { get; set; }

    /// <summary>
    /// Whether the wallet supports clone authorization, which allows
    /// one authorization context to extend to another app instance.
    /// Null if the wallet does not report this capability.
    /// </summary>
    [JsonProperty("supports_clone_authorization")]
    public bool? SupportsCloneAuthorization { get; set; }

    [Preserve]
    public WalletCapabilities() { }
}
