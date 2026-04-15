using Newtonsoft.Json;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

[Preserve]
public class CapabilitiesResult
{
    [JsonProperty("supports_clone_authorization")]
    public bool? SupportsCloneAuthorization { get; set; }

    [JsonProperty("max_transactions_per_request")]
    public int? MaxTransactionsPerRequest { get; set; }

    [JsonProperty("max_messages_per_request")]
    public int? MaxMessagesPerRequest { get; set; }

    [JsonProperty("supported_transaction_versions")]
    public string[] SupportedTransactionVersions { get; set; }

    [JsonProperty("features", NullValueHandling = NullValueHandling.Ignore)]
    public string[] Features { get; set; }
}
