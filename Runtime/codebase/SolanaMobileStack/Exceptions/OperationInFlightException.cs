using System;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class OperationInFlightException : InvalidOperationException
    {
        public OperationInFlightException(string message) : base(message) { }
    }
}
