using System.Collections.Generic;
#nullable enable
public class ChainConfig {
    public Web3Auth.ChainNamespace? chainNamespace { get; set; } = Web3Auth.ChainNamespace.EIP155;
    public int decimals { get; set; } = 18;
    public string blockExplorerUrl { get; set; } = null;
    public string chainId { get; set; }
    public string displayName { get; set; } = null;
    public string logo { get; set; } = null;
    public string rpcTarget { get; set; }
    public string ticker { get; set; } = null;
    public string tickerName { get; set; } = null;
}