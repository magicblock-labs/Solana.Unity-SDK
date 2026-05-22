using System;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class InvalidAuthorizationException : Exception
    {
        public InvalidAuthorizationException(string message) : base(message) { }

        public InvalidAuthorizationException(string message, Exception inner) : base(message, inner) { }
    }
}
