using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SolanaMobileStack.JsonRpcClient.Responses;

namespace SolanaMobileStack.Interfaces
{
    public interface IAdapterOperations
    {
        public Task<AuthorizationResult> Authorize(Uri identityUri, Uri iconUri, string identityName, string rpcCluster);
        public Task<AuthorizationResult> Reauthorize(Uri identityUri, Uri iconUri, string identityName, string authToken);
        public Task<SignedResult> SignTransactions(IEnumerable<byte[]> transactions);
        public Task<SignedResult> SignMessages(IEnumerable<byte[]> messages, IEnumerable<byte[]> addresses);
    }
}