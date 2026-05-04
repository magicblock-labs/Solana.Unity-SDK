using System;

namespace Solana.Unity.SolanaMobileStack
{
    public class ChainMismatchException : Exception
    {
        public string CachedChain { get; }
        public string RequestedChain { get; }

        public ChainMismatchException(string cachedChain, string requestedChain)
            : base($"Previously authorized on {cachedChain}. " +
                   $"Switch wallet to {cachedChain}, deauthorize, " +
                   $"then switch to {requestedChain} and connect.")
        {
            CachedChain = cachedChain;
            RequestedChain = requestedChain;
        }
    }
}
