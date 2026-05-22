using System;
using Solana.Unity.Rpc.Models;

namespace Solana.Unity.SolanaMobileStack
{
    public sealed class LegacyTransactionPayload : ITransactionPayload
    {
        private readonly Transaction _transaction;

        public LegacyTransactionPayload(Transaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public string ToBase64Payload() => Convert.ToBase64String(_transaction.Serialize());
    }
}
