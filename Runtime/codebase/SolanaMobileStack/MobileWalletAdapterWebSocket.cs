using System;
using NativeWebSocket;
using Solana.Unity.SDK;

// ReSharper disable once CheckNamespace

public class MobileWalletAdapterWebSocket: IMessageSender
{
    private readonly IWebSocket _webSocket;
    private readonly MobileWalletAdapterSession _session;

    public MobileWalletAdapterWebSocket(IWebSocket webSocket, MobileWalletAdapterSession session)
    {
        _webSocket = webSocket;
        _session = session;
    }

    public void Send(byte[] message)
    {
        if(message == null || message.Length == 0)
            throw new ArgumentException("Message cannot be null or empty");
        var encryptedMessage = _session.EncryptSessionPayload(message);
        _webSocket.Send(encryptedMessage);
        _webSocket.DispatchMessageQueue();
    }
}