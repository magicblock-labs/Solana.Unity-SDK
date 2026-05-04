using Solana.Unity.Rpc.Types;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class SendOptions
    {
        public Commitment? Commitment { get; set; }

        public bool? SkipPreflight { get; set; }

        public ulong? MinContextSlot { get; set; }

        public uint? MaxRetries { get; set; }

        public bool? WaitForCommitmentToSendNextTransaction { get; set; }
    }
}
