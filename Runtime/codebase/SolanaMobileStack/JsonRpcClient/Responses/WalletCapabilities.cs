using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

/// <summary>
/// Represents the capabilities reported by a connected wallet via the get_capabilities MWA endpoint.
/// </summary>
[Preserve]
public class WalletCapabilities
{
    /// <summary>
    /// Maximum number of transaction payloads that can be signed in a single request.
    /// Null if the wallet does not report this capability.
    /// </summary>
    [JsonProperty("max_transactions_per_request")]
    [RequiredMember]
    public int? MaxTransactionsPerRequest { get; set; }

    /// <summary>
    /// Maximum number of message payloads that can be signed in a single request.
    /// Null if the wallet does not report this capability.
    /// </summary>
    [JsonProperty("max_messages_per_request")]
    [RequiredMember]
    public int? MaxMessagesPerRequest { get; set; }

    /// <summary>
    /// Supported MWA feature set identifiers (e.g. "sign_and_send_transactions").
    /// </summary>
    [JsonProperty("supported_transaction_versions")]
    [RequiredMember]
    public List<string> SupportedTransactionVersions { get; set; }

    /// <summary>
    /// Whether the wallet supports the sign_and_send_transactions endpoint.
    /// </summary>
    [JsonProperty("supports_clone_authorization")]
    [RequiredMember]
    public bool? SupportsCloneAuthorization { get; set; }

    [Preserve]
    public WalletCapabilities() { }
}
