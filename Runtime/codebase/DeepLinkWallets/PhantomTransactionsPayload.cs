using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace  Solana.Unity.SDK
{
    [Serializable]
    public class PhantomTransactionsPayload
    {
        public List<string> transactions;
        public string session;

        public PhantomTransactionsPayload(List<string> transactions, string session)
        {
            this.transactions = transactions;
            this.session = session;
        }
    }
}