using System.Collections.Generic;
using Solana.Unity.SDK;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Mocks
{
    public class MockMessageSender : IMessageSender
    {
        public readonly List<byte[]> SentMessages = new List<byte[]>();

        public void Send(byte[] message)
        {
            SentMessages.Add(message == null ? null : (byte[])message.Clone());
        }

        public byte[] LastMessage => SentMessages.Count > 0
            ? (SentMessages[SentMessages.Count - 1] == null
                ? null
                : (byte[])SentMessages[SentMessages.Count - 1].Clone())
            : null;
    }
}
