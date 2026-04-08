using System.Collections.Generic;
using Solana.Unity.SDK;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK.Tests.EditMode.Mocks
{
    /// <summary>
    /// Simple test sender that records every message passed to Send().
    /// Tests can inspect the captured payloads afterward.
    /// </summary>
    public class MockMessageSender : IMessageSender
    {
        public readonly List<byte[]> SentMessages = new List<byte[]>();

        public void Send(byte[] message)
        {
            SentMessages.Add(message);
        }

        /// <summary>
        /// Returns the latest message, or null if nothing has been sent yet.
        /// </summary>
        public byte[] LastMessage => SentMessages.Count > 0
            ? SentMessages[SentMessages.Count - 1]
            : null;
    }
}
