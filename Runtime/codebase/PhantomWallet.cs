using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Merkator.BitCoin;
using Solana.Unity.Rpc.Models;
using UnityEngine;
using UnityEngine.Networking;
using X25519;
using Org.BouncyCastle.Security;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Wallet;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace Solana.Unity.SDK
{
    // TODO: App signing transaction without sending for deeplinks.
    public class PhantomWallet : WalletBase
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void ExternConnectPhantom();

        [DllImport("__Internal")]
        private static extern void ExternSignTransaction(string transaction);

        [DllImport("__Internal")]
        private static extern void ExternSignAndSendTransaction(string transaction);
#endif

        public string PhantomApiVersion = "v1";
        public string AppMetaDataUrl = "https://beavercrush.com";
        public string DeeplinkUrlSceme = "SolPlay";

        private string _sessionId;
        private X25519KeyPair _localKeyPairForPhantomConnection;
        private string _base58PublicKey = "";
        private string _phantomEncryptionPubKey;
        private Transaction currentTransaction;

        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<RequestResult<string>> _signAndSendTaskCompletionSource;
        private TaskCompletionSource<byte[]> _signedTransactionTaskCompletionSource;

        public override void Setup()
        {
#if UNITY_IOS || UNITY_ANROID
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }

            CreatePhantomAppEncryptionKeys();
#endif
        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();

#if UNITY_WEBGL
            ExternConnectPhantom();
#endif
#if UNITY_IOS || UNITY_ANROID
            DeeplinkLogin();
#endif
            return _loginTaskCompletionSource.Task;
        }

        private void DeeplinkLogin()
        {
            string appMetaDataUrl = AppMetaDataUrl;
            string redirectUri = UnityWebRequest.EscapeURL($"{DeeplinkUrlSceme}://onPhantomConnected");
            string url =
                $"https://phantom.app/ul/{PhantomApiVersion}/connect?app_url={appMetaDataUrl}&dapp_encryption_public_key={_base58PublicKey}&redirect_link={redirectUri}&cluster={GetClusterString()}";
            Application.OpenURL(url);
        }

        /// <summary>
        /// Can't create a new account in phantom wallet.
        /// </summary>
        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently only works for the java script phantom connection.
        /// </summary>
        public override Task<byte[]> SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<byte[]>();

#if UNITY_WEBGL
            var encode = Base58Encoding.Encode(transaction.CompileMessage());
            currentTransaction = transaction;
            ExternSignTransaction(encode);
#endif

#if UNITY_IOS || UNITY_ANROID
            throw new NotImplementedException();
#endif

            return _signedTransactionTaskCompletionSource.Task;
        }

        public override async Task<RequestResult<string>> SignAndSendTransaction(Transaction transaction)
        {
            _signAndSendTaskCompletionSource = new TaskCompletionSource<RequestResult<string>>();

#if UNITY_WEBGL
            var encode = Base58Encoding.Encode(transaction.CompileMessage());
            ExternSignAndSendTransaction(encode);
#endif

#if UNITY_IOS || UNITY_ANROID
            DeeplinkSignAndSendTransaction(transaction);
#endif

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

        public void OpenUrlInWalletBrowser(string url)
        {
#if UNITY_IOS || UNITY_ANROID
            string refUrl = UnityWebRequest.EscapeURL(GetAppMetaDataUrl());
            string escapedUrl = UnityWebRequest.EscapeURL(url);
            string inWalletUrl = $"https://phantom.app/ul/browse/{url}?ref=refUrl";
#else
            string inWalletUrl = url;
#endif
            Application.OpenURL(inWalletUrl);
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

        private void DeeplinkSignAndSendTransaction(Transaction transaction)
        {
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
                $"https://phantom.app/ul/v1/signAndSendTransaction?dapp_encryption_public_key={_base58PublicKey}&redirect_link={redirectUri}&nonce={Base58Encoding.Encode(randomNonce)}&payload={base58Payload}&cluster={GetClusterString()}";

            Debug.Log(url);
            Application.OpenURL(url);
        }

        /// <summary>
        /// This will be called from the mobile phantom wallet.
        /// </summary>
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
                Debug.LogError($"Deeplink error: {errorMessage}");
                _loginTaskCompletionSource.SetResult(null);
                return;
            }

            if (string.IsNullOrEmpty(data))
            {
                Debug.LogError("Phantom connect canceled.");
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
                _sessionId = connectSuccess.session;
                _loginTaskCompletionSource.SetResult(new Account(string.Empty, connectSuccess.public_key));
            }
            else
            {
                if (!string.IsNullOrEmpty(error.errorCode))
                {
                    _loginTaskCompletionSource.SetResult(null);
                    Debug.LogError($"Deeplink error: {error.errorCode} {error.errorMessage}");
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
                Debug.LogError($"Deeplink error: Error: {errorMessage} + Data: {data}");
                return;
            }

            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58Encoding.Decode(data),
                Base58Encoding.Decode(nonce), Base58Encoding.Decode(_phantomEncryptionPubKey),
                _localKeyPairForPhantomConnection.PrivateKey);
            string bytesToUtf8String = Encoding.UTF8.GetString(uncryptedMessage);

            PhantomWalletTransactionSuccessful success =
                JsonUtility.FromJson<PhantomWalletTransactionSuccessful>(bytesToUtf8String);

            _signAndSendTaskCompletionSource.SetResult(new RequestResult<string>(new HttpResponseMessage(),
                success.signature));
        }

        /// <summary>
        /// Called from java script when the phantom wallet approves the connection
        /// </summary>
        public void OnPhantomConnected(string walletPubKey)
        {
            Debug.Log($"Wallet {walletPubKey} connected!");
            _loginTaskCompletionSource.SetResult(new Account("", walletPubKey));
        }

        /// <summary>
        /// Called from java script when the phantom wallet signed the transaction and return the signature
        /// that we then need to put into the transaction before we send it out.
        /// </summary>
        public void OnTransactionSigned(string signature)
        {
            currentTransaction.Signatures.Add(new SignaturePubKeyPair()
            {
                PublicKey = Account.PublicKey,
                Signature = Base58Encoding.Decode(signature)
            });
            _signedTransactionTaskCompletionSource.SetResult(currentTransaction.Serialize());
        }

        /// <summary>
        /// Called from java script when the phantom wallet signed and send the transaction.
        /// </summary>
        public void OnTransactionSignedAndSent(string signature)
        {
            Debug.Log($"Signed and sent transaction with signature: {signature}");
            _signAndSendTaskCompletionSource.SetResult(new RequestResult<string>(new HttpResponseMessage(), signature));
        }

        private void CreatePhantomAppEncryptionKeys()
        {
            _localKeyPairForPhantomConnection = X25519KeyAgreement.GenerateKeyPair();
            _base58PublicKey = Base58Encoding.Encode(_localKeyPairForPhantomConnection.PublicKey);
        }
        
        private string GetClusterString()
        {
            switch (rpcCluster)
            {
                case RpcCluster.MainNet:
                    return "mainnet-beta";
                case RpcCluster.DevNet:
                    return "devnet";
                case RpcCluster.TestNet:
                    return "testnet";
            }
            
            return "mainnet-beta";
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