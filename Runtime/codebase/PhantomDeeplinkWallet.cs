using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Merkator.BitCoin;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using UnityEngine;
using UnityEngine.Networking;
using X25519;
using Org.BouncyCastle.Security;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Wallet;

namespace Solana.Unity.DeeplinkWallet
{
    // TODO: App signing transaction without sending.
    // TODO: Improve deeplink url parsing
    public class PhantomDeeplinkWallet : WalletBase
    {
        public string PhantomApiVersion = "v1";
        public string AppMetaDataUrl = "https://beavercrush.com";
        public string DeeplinkUrlSceme = "SolPlay";
        public event Action<IDeeplinkWallet.DeeplinkWalletError> OnDeeplinkWalletError;
        public event Action<IDeeplinkWallet.DeeplinkWalletTransactionSuccessful> OnDeeplinkTransactionSuccessful;

        private string _phantomWalletPublicKey;
        private string _sessionId;
        
        private X25519KeyPair _localKeyPairForPhantomConnection;
        private string _base58PublicKey = "";
        private string _phantomEncryptionPubKey;
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<RequestResult<string>> _signAndSendTaskCompletionSource;

        public override void Setup()
        {
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }

            CreateEncryptionKeys();
        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            string appMetaDataUrl = AppMetaDataUrl;
            string redirectUri = UnityWebRequest.EscapeURL($"{DeeplinkUrlSceme}://onPhantomConnected");
            string url =
                $"https://phantom.app/ul/{PhantomApiVersion}/connect?app_url={appMetaDataUrl}&dapp_encryption_public_key={_base58PublicKey}&redirect_link={redirectUri}";

            Application.OpenURL(url);
            return _loginTaskCompletionSource.Task;
        }

        /// <summary>
        /// Can't connect a new account in phantom wallet.
        /// </summary>
        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Will come soon
        /// </summary>
        public override Task<byte[]> SignTransaction(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        public override async Task<RequestResult<string>> SignAndSendTransaction(Transaction transaction)
        {
            var blockHash = await ActiveRpcClient.GetRecentBlockHashAsync();
            _signAndSendTaskCompletionSource = new TaskCompletionSource<RequestResult<string>>();
            
            if (blockHash.Result == null)
            {
                var blockHashCouldNotBeCreatedError = "Block hash null. Connected to internet?";
                OnDeeplinkWalletError?.Invoke(
                    new IDeeplinkWallet.DeeplinkWalletError("0", blockHashCouldNotBeCreatedError));
                RequestResult<string> result = new RequestResult<string>();
                result.Reason = blockHashCouldNotBeCreatedError;
                _signAndSendTaskCompletionSource.SetResult(result);
                return _signAndSendTaskCompletionSource.Task.Result;
            }

            string redirectUri = $"{DeeplinkUrlSceme}://transactionSuccessful";

            byte[] serializedTransaction = transaction.Serialize();
            string base58Transaction = Base58Encoding.Encode(serializedTransaction);

            PhantomTransactionPayload transactionPayload = new PhantomTransactionPayload(base58Transaction, _sessionId);
            string transactionPayloadJson = JsonUtility.ToJson(transactionPayload);

            byte[] bytesJson = Encoding.UTF8.GetBytes(transactionPayloadJson);

            byte[] randomNonce = GenerateRandomBytes(24);
            
            byte[] encryptedMessage = TweetNaCl.TweetNaCl.CryptoBox(bytesJson, randomNonce,
                Base58Encoding.Decode(_phantomEncryptionPubKey), _localKeyPairForPhantomConnection.PrivateKey);
            
            string base58Payload = Base58Encoding.Encode(encryptedMessage);

            string url =
                $"https://phantom.app/ul/v1/signAndSendTransaction?dapp_encryption_public_key={_base58PublicKey}&redirect_link={redirectUri}&nonce={Base58Encoding.Encode(randomNonce)}&payload={base58Payload}";

            Application.OpenURL(url);
            
            await _signAndSendTaskCompletionSource.Task;
            return _signAndSendTaskCompletionSource.Task.Result;
        }

        public bool TryGetSessionId(out string session)
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                session = _sessionId;
                return true;
            }

            session = "";
            return false;
        }

        public string GetAppMetaDataUrl()
        {
            return AppMetaDataUrl;
        }

        private byte[] GenerateRandomBytes(int size)
        {
            byte[] buffer = new byte[size];
            new SecureRandom().NextBytes(buffer);
            return buffer;
        }
        
        public void OpenUrlInWalletBrowser(string url)
        {
#if UNITY_EDITOR || UNITY_WEBGL
            string inWalletUrl = url;
#else
            string refUrl = UnityWebRequest.EscapeURL(GetAppMetaDataUrl());
            string escapedUrl = UnityWebRequest.EscapeURL(url);
            string inWalletUrl = $"https://phantom.app/ul/browse/{url}?ref=refUrl";
#endif
            Application.OpenURL(inWalletUrl);
        }

        private void OnDeepLinkActivated(string url)
        {
            if (url.Contains("transactionSuccessful"))
            {
                ParseSuccessfulTransaction(url);
                return;
            }

            ParseConnectionSuccessful(url);
        }

        private void ParseConnectionSuccessful(string url)
        {
            string phantomResponse = url.Split("?"[0])[1];

            NameValueCollection result = HttpUtility.ParseQueryString(phantomResponse);
            _phantomEncryptionPubKey = result.Get("phantom_encryption_public_key");

            string phantomNonce = result.Get("nonce");
            string data = result.Get("data");
            string errorMessage = result.Get("errorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                OnDeeplinkWalletError?.Invoke(new IDeeplinkWallet.DeeplinkWalletError("0", errorMessage));
                _loginTaskCompletionSource.SetResult(null);
                return;
            }

            if (string.IsNullOrEmpty(data))
            {
                OnDeeplinkWalletError?.Invoke(
                    new IDeeplinkWallet.DeeplinkWalletError("0", "Phantom connect canceled."));
                _loginTaskCompletionSource.SetResult(null);
                return;
            }
            
            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58Encoding.Decode(data),
                Base58Encoding.Decode(phantomNonce), Base58Encoding.Decode(_phantomEncryptionPubKey),
                _localKeyPairForPhantomConnection.PrivateKey);

            string bytesToUtf8String = Encoding.UTF8.GetString(uncryptedMessage);

            PhantomWalletConnectSuccess connectSuccess =
                JsonUtility.FromJson<PhantomWalletConnectSuccess>(bytesToUtf8String);
            PhantomWalletError error = JsonUtility.FromJson<PhantomWalletError>(bytesToUtf8String);

            if (!string.IsNullOrEmpty(connectSuccess.public_key))
            {
                _phantomWalletPublicKey = connectSuccess.public_key;
                _sessionId = connectSuccess.session;
                _loginTaskCompletionSource.SetResult(new Account(string.Empty, connectSuccess.public_key));
            }
            else
            {
                if (!string.IsNullOrEmpty(error.errorCode))
                {
                    _loginTaskCompletionSource.SetResult(null);
                    OnDeeplinkWalletError?.Invoke(
                        new IDeeplinkWallet.DeeplinkWalletError(error.errorCode, error.errorMessage));
                }
            }
        }

        private void ParseSuccessfulTransaction(string url)
        {
            string phantomResponse = url.Split("?"[0])[1];

            NameValueCollection result = HttpUtility.ParseQueryString(phantomResponse);
            string nonce = result.Get("nonce");
            string data = result.Get("data");
            string errorMessage = result.Get("errorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                OnDeeplinkWalletError?.Invoke(
                    new IDeeplinkWallet.DeeplinkWalletError("0", $"Error: {errorMessage} + Data: {data}"));
                return;
            }

            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58Encoding.Decode(data), Base58Encoding.Decode(nonce),
                Base58Encoding.Decode(_phantomEncryptionPubKey), _localKeyPairForPhantomConnection.PrivateKey);
            string bytesToUtf8String = Encoding.UTF8.GetString(uncryptedMessage);

            PhantomWalletTransactionSuccessful success =
                JsonUtility.FromJson<PhantomWalletTransactionSuccessful>(bytesToUtf8String);

            OnDeeplinkTransactionSuccessful?.Invoke(
                new IDeeplinkWallet.DeeplinkWalletTransactionSuccessful(success.signature));
        }

        private void CreateEncryptionKeys()
        {
            _localKeyPairForPhantomConnection = X25519KeyAgreement.GenerateKeyPair();
            _base58PublicKey = Base58Encoding.Encode(_localKeyPairForPhantomConnection.PublicKey);
        }

        [Serializable]
        private class PhantomTransactionPayload
        {
            public string transaction;
            public string session;

            public PhantomTransactionPayload(string transaction, string session)
            {
                this.transaction = transaction;
                this.session = session;
            }
        }

        [Serializable]
        public class PhantomWalletError
        {
            public string errorCode;
            public string errorMessage;
        }

        [Serializable]
        public class PhantomWalletConnectSuccess
        {
            public string public_key;
            public string session;
        }

        [Serializable]
        public class PhantomWalletTransactionSuccessful
        {
            public string signature;
        }
    }
}