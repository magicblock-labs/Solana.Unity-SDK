using System;

namespace Solana.Unity.SolanaMobileStack
{
    public abstract class DeauthorizeResult
    {
        public sealed class FullyRevoked : DeauthorizeResult { }

        public sealed class LocalOnly : DeauthorizeResult
        {
            public string WalletPackage { get; set; }
        }

        public sealed class Failed : DeauthorizeResult
        {
            public Exception Error { get; set; }
        }
    }
}
