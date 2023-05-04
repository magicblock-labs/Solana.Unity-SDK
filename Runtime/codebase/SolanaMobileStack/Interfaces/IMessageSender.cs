using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    [Preserve]
    public interface IMessageSender
    {
        void Send(byte[] message);
    }
}