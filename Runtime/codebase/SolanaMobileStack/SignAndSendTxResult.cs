namespace Solana.Unity.SolanaMobileStack
{
    public abstract class SignAndSendTxResult
    {
        public sealed class Success : SignAndSendTxResult
        {
            public byte[][] Signatures { get; set; }
        }

        public sealed class UserDenied : SignAndSendTxResult { }

        public sealed class InvalidPayloads : SignAndSendTxResult
        {
            public bool[] Valid { get; set; }
        }

        public sealed class NotSubmitted : SignAndSendTxResult
        {
            public byte[][] PartialSignatures { get; set; }
        }

        public sealed class TooManyPayloads : SignAndSendTxResult
        {
            public uint? MaxTransactionsPerRequest { get; set; }
        }

        public sealed class ChainNotSupported : SignAndSendTxResult { }

        public sealed class AuthRevoked : SignAndSendTxResult { }

        public sealed class WalletUnreachable : SignAndSendTxResult { }
    }
}
