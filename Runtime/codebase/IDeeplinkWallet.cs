using System;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;

namespace Solana.Unity.DeeplinkWallet
{
    public interface IDeeplinkWallet
    {
        event Action<string> OnDeepLinkTriggered;
        event Action<DeeplinkWalletConnectSuccess> OnDeeplinkWalletConnectionSuccess;
        event Action<DeeplinkWalletError> OnDeeplinkWalletError;
        event Action<DeeplinkWalletTransactionSuccessful> OnDeeplinkTransactionSuccessful;

        /// <summary>
        /// Initialize the wallet and then call connect to create a secure connection with the wallet via deeplinks.
        /// </summary>
        /// <param name="deeplinkUrlSceme">This sceme you need to configure in the player setting for ios and in
        /// the android manifest for android.</param>
        /// <param name="rpcClient">Create it before and assign it here. Its used to get the current block hash for transacitons</param>
        /// <param name="appMetaDataUrl">The icon and text here will be shown in the popup in phantom</param>
        /// <param name="apiVersion">The api version of the deelink api</param>
        void Init(string deeplinkUrlSceme, IRpcClient rpcClient, string appMetaDataUrl, string apiVersion = "v1");

        /// <summary>
        /// This establish a secure connection to the wallet and then trigger either DeeplinkWalletConnectSuccess or DeeplinkWalletError.
        /// </summary>
        void Connect();

        bool TryGetWalletPublicKey(out string phantomPublicKey);
        bool TryGetSessionId(out string session);
        string GetAppMetaDataUrl();

        /// <summary>
        /// You need to create an unsigned transaction and pass it in here, it will then be serialized and sent to the wallet
        /// for signing and sending. Then it will either trigger OnDeeplinkTransactionSuccessful or DeeplinkWalletError.
        /// </summary>
        /// <param name="transaction"></param>
        void SignAndSendTransaction(Transaction transaction);

        /// <summary>
        /// Open a webpage within the wallet for example for NFT mint or raydium token swap.
        /// For example: https://raydium.io/swap/?inputCurrency=sol&outputCurrency=PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P&inputAmount=0.1&outputAmount=0.9&fixed=in
        /// </summary>
        /// <param name="url"></param>
        void OpenUrlInWalletBrowser(string url);

        public class DeeplinkWalletError
        {
            public string ErrorCode;
            public string ErrorMessage;

            public DeeplinkWalletError(string errorCode, string errorMessage)
            {
                ErrorMessage = errorMessage;
                ErrorCode = errorCode;
            }
        }

        public class DeeplinkWalletConnectSuccess
        {
            public string PublicKey;
            public string Session;

            public DeeplinkWalletConnectSuccess(string publicKey, string session)
            {
                PublicKey = publicKey;
                Session = session;
            }
        }

        public class DeeplinkWalletTransactionSuccessful
        {
            public string Signature;

            public DeeplinkWalletTransactionSuccessful(string signature)
            {
                Signature = signature;
            }
        }
    }
}