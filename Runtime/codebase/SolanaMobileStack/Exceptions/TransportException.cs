using System;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class TransportException : Exception
    {
        public string Operation { get; }

        public TransportException(string operation, Exception? inner = null)
            : base($"Transport failure during '{operation}'", inner)
        {
            Operation = operation;
        }
    }
}
