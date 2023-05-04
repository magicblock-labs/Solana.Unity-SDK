// ReSharper disable once CheckNamespace

public static class WebSocketsTransportContract
{
    public const string WebsocketsLocalScheme = "ws";
    public const string WebsocketsLocalHost = "127.0.0.1";
    public const string WebsocketsLocalPath = "/solana-wallet";
    public const int WebsocketsLocalPortMin = 49152;
    public const int WebsocketsLocalPortMax = 65535;
    public const string WebsocketsRelectorScheme = "wss";
    public const string WebsocketsProtocol = "com.solana.mobilewalletadapter.v1";
}
