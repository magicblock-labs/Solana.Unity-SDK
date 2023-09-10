using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Solana.Unity.SDK.Editor
{
    internal class BundlrClient 
    {

        #region Constants

        /// <summary>
        /// URI format for retrieving upload fee by size.
        /// </summary>
        const string BUNDLR_FEE_URI_FORMAT = "{0}/price/solana/{1}";

        /// <summary>
        /// URI format for retrieving a Bundlr address.
        /// </summary>
        const string BUNDLR_ADDRESS_URI_FORMAT = "{0}/info";

        /// <summary>
        /// URI format for retrieving a Bundlr balance for a specified account.
        /// </summary>
        const string BUNDLR_BALANCE_URI_FORMAT = "{0}/account/balance/solana/?address={1}";

        /// <summary>
        /// URI format for sending a Bundlr transaction.
        /// </summary>
        const string BUNDLR_TRX_URI_FORMAT = "{0}/tx/solana";

        #endregion

        #region Fields

        private readonly string bundlrNode;
        private readonly HttpClient httpClient = new();
        private readonly Account signer;

        #endregion

        #region Constructors

        internal BundlrClient(string bundlrNode, Account signer)
        {
            this.bundlrNode = bundlrNode;
            this.signer = signer;
        }

        #endregion

        #region Internal

        internal async Task<string> SendTransaction(BundlrUploadTransaction tx)
        {
            tx.Sign(signer);
            var data = tx.Serialize();
            var uri = string.Format(BUNDLR_TRX_URI_FORMAT, bundlrNode);
            try {
                var content = new ByteArrayContent(data);
                content.Headers.ContentType = new("application/octet-stream");
                var response = await httpClient.PostAsync(
                    uri,
                    content
                );
                var responseJson = await response.Content.ReadAsStringAsync();
                var id = JsonConvert.DeserializeObject<JObject>(responseJson)["id"];
                return id.ToString();
            } catch (Exception ex) {
                Debug.Log(ex);
                return null;
            }
        }

        internal async Task<ulong> GetBundlrFee(ulong uploadSize)
        {
            var requestUri = string.Format(BUNDLR_FEE_URI_FORMAT, bundlrNode, uploadSize);
            var response = await httpClient.GetStringAsync(requestUri);
            return ulong.Parse(response);
        }

        internal async Task<ulong> GetBundlrBalance(PublicKey account)
        {
            Debug.LogFormat("Getting Bundlr balance for address: {0}", account);
            var requestUri = string.Format(BUNDLR_BALANCE_URI_FORMAT, bundlrNode, account);
            var response = await httpClient.GetStringAsync(requestUri);
            var balanceString = JsonConvert.DeserializeObject<JObject>(response)["balance"];
            return ulong.Parse(balanceString.ToString());
        }

        internal async Task<string> GetBundlrSolanaAddress()
        {
            var uri = string.Format(BUNDLR_ADDRESS_URI_FORMAT, bundlrNode);
            var response = await httpClient.GetStringAsync(uri);
            if (response == null) {
                throw new Exception("Could you retrieve Bundlr address.");
            }
            var addresses = JsonConvert.DeserializeObject<JObject>(response)["addresses"];
            return addresses["solana"].ToString();
        }

        internal async Task<bool> FundBundlrAddress(
            Account payer,
            IRpcClient rpcClient,
            string bundlrAddress,
            ulong amount
        )
        {
            var recentBlockhash = await rpcClient.GetLatestBlockHashAsync();
            var transaction = new TransactionBuilder()
                .AddInstruction(
                    SystemProgram.Transfer(
                        payer,
                        new(bundlrAddress),
                        amount
                    )
                )
                .SetRecentBlockHash(recentBlockhash.Result.Value.Blockhash)
                .SetFeePayer(payer);
            var tx = Transaction.Deserialize(transaction.Serialize());
            tx.PartialSign(payer);

            Debug.LogFormat("Funding Bundlr address from: {0}", payer.PublicKey);
            var response = await rpcClient.SendTransactionAsync(tx.Serialize(), commitment: Rpc.Types.Commitment.Confirmed);
            return response.Result != null;
        }

        #endregion
    }
}
