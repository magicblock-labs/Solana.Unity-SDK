using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Chaos.NaCl;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using UnityEngine;
using UnityEngine.Networking;
using X25519;
using Merkator.BitCoin;
using Org.BouncyCastle.Security;
using Solana.Unity.KeyStore.Crypto;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Wallet;

namespace Solana.Unity.DeeplinkWallet
{
    // TODO: Add a transaction counter to be able to keep track across multiple transactions
    // TODO: App signing transaction without sending.
    // TODO: Improve deeplink url parsing
    public class PhantomDeeplinkWallet : WalletBaseInterface
    {
        public event Action<string> OnDeepLinkTriggered;
        public event Action<IDeeplinkWallet.DeeplinkWalletConnectSuccess> OnDeeplinkWalletConnectionSuccess;
        public event Action<IDeeplinkWallet.DeeplinkWalletError> OnDeeplinkWalletError;
        public event Action<IDeeplinkWallet.DeeplinkWalletTransactionSuccessful> OnDeeplinkTransactionSuccessful;

        private string _phantomWalletPublicKey;
        private string _sessionId;
        
        private X25519KeyPair _localKeyPairForPhantomConnection;
        private string _base58PublicKey = "";
        private string _phantomEncryptionPubKey;
        private string _phantomApiVersion = "v1";
        private string _appMetaDataUrl = "https://beavercrush.com";
        private string _deeplinkUrlSceme = "SolPlay";
        private IRpcClient _rpcClient;

        public void Setup(string deeplinkUrlSceme, IRpcClient rpcClient, string appMetaDataUrl, string apiVersion = "v1")
        {
            _phantomApiVersion = apiVersion;

            _deeplinkUrlSceme = deeplinkUrlSceme;
            _appMetaDataUrl = appMetaDataUrl;
            _rpcClient = rpcClient;

            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }

            CreateEncryptionKeys();
        }

        public void Setup()
        {
            throw new NotImplementedException();
        }

        private TaskCompletionSource<Account> _loginTaskCompletionSource;

        public Task<Account> Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            string appMetaDataUrl = this._appMetaDataUrl;
            string redirectUri = UnityWebRequest.EscapeURL($"{_deeplinkUrlSceme}://onPhantomConnected");
            string url =
                $"https://phantom.app/ul/{_phantomApiVersion}/connect?app_url={appMetaDataUrl}&dapp_encryption_public_key={_base58PublicKey}&redirect_link={redirectUri}";

            Application.OpenURL(url);
            return _loginTaskCompletionSource.Task;
        }

        public bool TryGetWalletPublicKey(out string phantomPublicKey)
        {
            if (!string.IsNullOrEmpty(_phantomWalletPublicKey))
            {
                phantomPublicKey = _phantomWalletPublicKey;
                return true;
            }

            phantomPublicKey = "";
            return false;
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
            return _appMetaDataUrl;
        }

        public async void SignAndSendTransaction(Transaction transaction)
        {
            var blockHash = await _rpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                OnDeeplinkWalletError?.Invoke(
                    new IDeeplinkWallet.DeeplinkWalletError("0", "Block hash null. Connected to internet?"));
                return;
            }

            string redirectUri = $"{_deeplinkUrlSceme}://transactionSuccessful";

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
                OnDeeplinkWalletConnectionSuccess?.Invoke(
                    new IDeeplinkWallet.DeeplinkWalletConnectSuccess(connectSuccess.public_key,
                        connectSuccess.session));
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

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public Task<Account> CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetBalance(PublicKey publicKey)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetBalance()
        {
            throw new NotImplementedException();
        }

        public Task<RequestResult<string>> Transfer(PublicKey destination, PublicKey tokenMint, ulong amount)
        {
            throw new NotImplementedException();
        }

        public Task<TokenAccount[]> GetTokenAccounts(PublicKey tokenMint, PublicKey tokenProgramPublicKey)
        {
            throw new NotImplementedException();
        }

        public Task<TokenAccount[]> GetTokenAccounts()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> SignTransaction(Transaction transaction)
        {
            throw new NotImplementedException();
        }
    }
}