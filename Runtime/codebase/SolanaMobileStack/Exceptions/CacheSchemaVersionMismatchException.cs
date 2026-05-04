using System;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class CacheSchemaVersionMismatchException : Exception
    {
        public int ExpectedSchemaVersion { get; }

        public int ActualSchemaVersion { get; }

        public CacheSchemaVersionMismatchException(int expected, int actual)
            : base($"Cache schema version mismatch: expected {expected}, got {actual}.")
        {
            ExpectedSchemaVersion = expected;
            ActualSchemaVersion = actual;
        }
    }
}
