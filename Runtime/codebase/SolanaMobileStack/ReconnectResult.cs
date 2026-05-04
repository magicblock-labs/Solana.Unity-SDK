using System;
using Solana.Unity.Wallet;

namespace Solana.Unity.SolanaMobileStack
{
    public abstract class ReconnectResult
    {
        public sealed class SilentSuccess : ReconnectResult
        {
            public Account Account { get; set; }
        }

        public sealed class NoCachedSession : ReconnectResult { }

        public sealed class Failed : ReconnectResult
        {
            public Exception Error { get; set; }
        }
    }
}
