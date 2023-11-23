using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Chaos.NaCl;
using JetBrains.Annotations;
using Merkator.Tools;
using Solana.Unity.KeyStore.Services;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;
using Pbkdf2Params = Solana.Unity.KeyStore.Model.Pbkdf2Params;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class PhantomDeepLink: WalletBase
    {
        private const string TempKpPrefEntry = "phantom-kp-session";
        private const string SessionIdPrefEntry = "phantom-session-id";
        private const string PhantomEncryptionPubKeyPrefEntry = "phantom-pk-encryption";
        private const string PhantomPublicKeyPrefEntry = "phantom-pk-session";
        
        private readonly PhantomWalletOptions _deepLinksWalletOptions;
        
        private static Account _tmpPhantomConnectionAccount = new();
        private static byte[] PhantomConnectionAccountPrivateKey 
            => ArrayHelpers.SubArray(_tmpPhantomConnectionAccount.PrivateKey.KeyBytes, 0, 32);
        private static byte[] PhantomConnectionAccountPublicKey =>
            MontgomeryCurve25519.GetPublicKey(PhantomConnectionAccountPrivateKey);
        
        private string _sessionId;
        private byte[] _phantomEncryptionPubKey;
        
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private TaskCompletionSource<Transaction[]> _signedAllTransactionsTaskCompletionSource;
        private TaskCompletionSource<byte[]> _signedMessageTaskCompletionSource;

        public PhantomDeepLink(
            PhantomWalletOptions deepLinksWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            _deepLinksWalletOptions = deepLinksWalletOptions;
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeepLinkActivated(Application.absoluteURL);
        }

        protected override Task<Account> _Login(string password = null)
        {
            var pk = PlayerPrefs.GetString(PhantomPublicKeyPrefEntry, null);
            if (!string.IsNullOrEmpty(pk))
            {
                try
                {
                    LoadSessionInfo(new PublicKey(pk));
                    return Task.FromResult(new Account(string.Empty, new PublicKey(pk)));
                }
                catch (Exception)
                {
                    Debug.LogError("Corrupted session info.");
                    DestroySessionInfo();
                }
            }
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            StartLogin();
            return _loginTaskCompletionSource.Task;
        }
        
        public override void Logout()
        {
            StartDisconnect();
            DestroySessionInfo();
            Application.deepLinkActivated -= OnDeepLinkActivated;
            base.Logout();
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            StartSignTransaction(transaction);
            return _signedTransactionTaskCompletionSource.Task;
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            _signedAllTransactionsTaskCompletionSource = new TaskCompletionSource<Transaction[]>();
            StartSignAllTransactions(transactions);
            return _signedAllTransactionsTaskCompletionSource.Task;
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            _signedMessageTaskCompletionSource = new TaskCompletionSource<byte[]>();
            StartSignMessage(message);
            return _signedMessageTaskCompletionSource.Task;
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }
        
        private RpcCluster GetCluster()
        {
            return RpcCluster switch
            {
                RpcCluster.DevNet => RpcCluster.DevNet,
                RpcCluster.TestNet => RpcCluster.TestNet,
                RpcCluster.MainNet => RpcCluster.MainNet,
                RpcCluster.LocalNet => RpcCluster.LocalNet,
                _ => RpcCluster.MainNet
            };
        }
        
        #region DeepLinks

        private void StartLogin()
        {
            var url = Utils.CreateLoginDeepLink(
                baseUrl: _deepLinksWalletOptions.BaseUrl,
                redirectScheme: _deepLinksWalletOptions.DeeplinkUrlScheme,
                metadataUrl: _deepLinksWalletOptions.AppMetaDataUrl,
                apiVersion: _deepLinksWalletOptions.ApiVersion,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
                cluster: GetCluster()
            );
            Application.OpenURL(url);
        }
        
        private void StartDisconnect()
        {
            var url = Utils.CreateDisconnectDeepLink(
                phantomEncryptionPubKey: _phantomEncryptionPubKey,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
                phantomConnectionAccountPrivateKey: PhantomConnectionAccountPrivateKey,
                baseUrl:  _deepLinksWalletOptions.BaseUrl,
                redirectScheme:  _deepLinksWalletOptions.DeeplinkUrlScheme,
                apiVersion: _deepLinksWalletOptions.ApiVersion,
                sessionId: _sessionId,
                cluster: GetCluster()
                
            );
            Application.OpenURL(url);
        }

        private void StartSignTransaction(Transaction transaction)
        {
            var url = Utils.CreateSignTransactionDeepLink(
                transaction: transaction,
                phantomEncryptionPubKey: _phantomEncryptionPubKey,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
                phantomConnectionAccountPrivateKey: PhantomConnectionAccountPrivateKey,
                baseUrl:  _deepLinksWalletOptions.BaseUrl,
                redirectScheme:  _deepLinksWalletOptions.DeeplinkUrlScheme,
                apiVersion: _deepLinksWalletOptions.ApiVersion,
                sessionId: _sessionId,
                cluster: GetCluster()
                
            );
            Application.OpenURL(url);
        }

        private void StartSignAllTransactions(Transaction[] transactions)
        {
            var url = Utils.CreateSignAllTransactionsDeepLink(
                transactions: transactions,
                phantomEncryptionPubKey: _phantomEncryptionPubKey,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
                phantomConnectionAccountPrivateKey: PhantomConnectionAccountPrivateKey,
                baseUrl: _deepLinksWalletOptions.BaseUrl,
                redirectScheme: _deepLinksWalletOptions.DeeplinkUrlScheme,
                apiVersion: _deepLinksWalletOptions.ApiVersion,
                sessionId: _sessionId,
                cluster: GetCluster()
            );
            Application.OpenURL(url);
        }
        
        private void StartSignMessage(byte[] message)
        {
            var url = Utils.CreateSignMessageDeepLink(
                message: message,
                phantomEncryptionPubKey: _phantomEncryptionPubKey,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
                phantomConnectionAccountPrivateKey: PhantomConnectionAccountPrivateKey,
                baseUrl:  _deepLinksWalletOptions.BaseUrl,
                redirectScheme:  _deepLinksWalletOptions.DeeplinkUrlScheme,
                apiVersion: _deepLinksWalletOptions.ApiVersion,
                sessionId: _sessionId,
                cluster: GetCluster()
                
            );
            Application.OpenURL(url);
        }
        
        #endregion        

        #region Callbacks

        private void OnDeepLinkActivated(string url)
        {
            if (url.ToLower().Contains("alltransactionssigned"))
            {
                ParseSuccessfullySignedAllTransactions(url);
            }
            else if (url.ToLower().Contains("transactionsigned"))
            {
                ParseSuccessfullySignedTransaction(url);
            }
            else if (url.ToLower().Contains("onconnected"))
            {
                ParseConnectionSuccessful(url);
            }
            else if (url.ToLower().Contains("messagesigned"))
            {
                ParseSuccessfullySignedMessage(url);
            }
            else if (url.ToLower().Contains("disconnect"))
            {
                Debug.LogError("on disconnect");
                DestroySessionInfo();
            }
        }

        private void ParseConnectionSuccessful(string url)
        {
            var result = ParseQueryString(url);
            _phantomEncryptionPubKey = Encoders.Base58.DecodeData(result[$"{_deepLinksWalletOptions.WalletName}_encryption_public_key"]);
            result.TryGetValue("nonce", out var phantomNonce);
            result.TryGetValue("data", out var data);
            result.TryGetValue("errorMessage", out var errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _loginTaskCompletionSource?.TrySetResult(null);
                return;
            }
            if (string.IsNullOrEmpty(data))
            {
                _loginTaskCompletionSource?.TrySetResult(null);
                return;
            }
            data = data.Replace("#", "");
            var k = MontgomeryCurve25519.KeyExchange(_phantomEncryptionPubKey, PhantomConnectionAccountPrivateKey);
            var unencryptedMessage = XSalsa20Poly1305.TryDecrypt(
                Encoders.Base58.DecodeData(data), k, Encoders.Base58.DecodeData(phantomNonce));
            var bytesToUtf8String = Encoding.UTF8.GetString(unencryptedMessage);
            var connectSuccess = JsonUtility.FromJson<PhantomWalletConnectSuccess>(bytesToUtf8String);
            var error = JsonUtility.FromJson<PhantomWalletError>(bytesToUtf8String);
            if (!string.IsNullOrEmpty(connectSuccess.public_key))
            {
                _sessionId = connectSuccess.session;
                var account = new Account(string.Empty, connectSuccess.public_key);
                _loginTaskCompletionSource?.TrySetResult(account);
                SaveSessionInfo(account.PublicKey);
            }
            else
            {
                if (string.IsNullOrEmpty(error.errorCode)) return;
                _loginTaskCompletionSource?.TrySetResult(null);
                Debug.LogError($"Deeplink error: {error.errorCode} {error.errorMessage}");
            }
        }
        
        private void ParseSuccessfullySignedAllTransactions(string url)
        {
            var result = ParseQueryString(url);
            result.TryGetValue("nonce", out var nonce);
            result.TryGetValue("data", out var data);
            result.TryGetValue("errorMessage", out var errorMessage);
            if (!string.IsNullOrEmpty(errorMessage) || string.IsNullOrEmpty(data))
            {
                result.TryGetValue("errorCode", out var errorCode);
                _signedAllTransactionsTaskCompletionSource?.TrySetResult(null);
                Debug.LogError($"Deeplink error: Error: {errorMessage} + Data: {data}");
                return;
            }

            data = data.Replace("#", "");
            var k = MontgomeryCurve25519.KeyExchange(_phantomEncryptionPubKey, PhantomConnectionAccountPrivateKey);
            var unencryptedMessage =
                XSalsa20Poly1305.TryDecrypt(Encoders.Base58.DecodeData(data), k, Encoders.Base58.DecodeData(nonce));
            var bytesToUtf8String = Encoding.UTF8.GetString(unencryptedMessage);
            var success = JsonUtility.FromJson<PhantomWalletAllTransactionsSignedSuccessfully>(bytesToUtf8String);
            var base58TransBytes = success.transactions.Select(x => Encoders.Base58.DecodeData(x));
            var transactions = base58TransBytes.Select(x => Transaction.Deserialize(x)).ToArray();
            _signedAllTransactionsTaskCompletionSource?.TrySetResult(transactions);
        }

        private void ParseSuccessfullySignedTransaction(string url)
        {
            var result = ParseQueryString(url);
            result.TryGetValue("nonce", out var nonce);
            result.TryGetValue("data", out var data);
            result.TryGetValue("errorMessage", out var errorMessage);
            if (!string.IsNullOrEmpty(errorMessage) || string.IsNullOrEmpty(data))
            {
                Debug.LogError($"Deeplink error: Error: {errorMessage} + Data: {data}");
                return;
            }
            data = data.Replace("#", "");
            var k = MontgomeryCurve25519.KeyExchange(_phantomEncryptionPubKey, PhantomConnectionAccountPrivateKey);
            var unencryptedMessage = XSalsa20Poly1305.TryDecrypt(Encoders.Base58.DecodeData(data), k, Encoders.Base58.DecodeData(nonce));
            var bytesToUtf8String = Encoding.UTF8.GetString(unencryptedMessage);
            var success = JsonUtility.FromJson<PhantomWalletTransactionSignedSuccessfully>(bytesToUtf8String);
            var base58TransBytes = Encoders.Base58.DecodeData(success.transaction);
            var transaction = Transaction.Deserialize(base58TransBytes);
            _signedTransactionTaskCompletionSource.SetResult(transaction);
        }

        private void ParseSuccessfullySignedMessage(string url)
        {
            var result = ParseQueryString(url);
            result.TryGetValue("nonce", out var nonce);
            result.TryGetValue("data", out var data);
            result.TryGetValue("errorMessage", out var errorMessage);
            if (!string.IsNullOrEmpty(errorMessage) || string.IsNullOrEmpty(data))
            {
                Debug.LogError($"Deeplink error: Error: {errorMessage} + Data: {data}");
                return;
            }
            data = data.Replace("#", "");
            var k = MontgomeryCurve25519.KeyExchange(_phantomEncryptionPubKey, PhantomConnectionAccountPrivateKey);
            var unencryptedMessage = XSalsa20Poly1305.TryDecrypt(Encoders.Base58.DecodeData(data), k, Encoders.Base58.DecodeData(nonce));
            var bytesToUtf8String = Encoding.UTF8.GetString(unencryptedMessage);
            var success = JsonUtility.FromJson<PhantomWalletMessageSignedSuccessfully>(bytesToUtf8String);
            var base58SigBytes = Encoders.Base58.DecodeData(success.signature);
            _signedMessageTaskCompletionSource.SetResult(base58SigBytes);
        }

        private static Dictionary<string, string> ParseQueryString(string url)
        {
            var querystring = url.Substring(url.IndexOf('?') + 1);
            var pairs = querystring.Split('&'); 
            var dict = pairs.Select(pair => {
                var valuePair = pair.Split('='); 
                return new KeyValuePair<string, string>(valuePair[0], valuePair[1]);
            }) .ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value); 
            return dict;
        }

        #endregion

        #region Utils
        
        private void SaveSessionInfo(PublicKey account)
        {
            var password = DeriveEncryptionPassword(Web3.Account.PublicKey);
            
            MainThreadDispatcher.Instance().Enqueue(
                SaveEncryptedSecret(
                    password,
                    _tmpPhantomConnectionAccount.PrivateKey.Key,
                    TempKpPrefEntry,
                    account));
            PlayerPrefs.SetString(SessionIdPrefEntry, _sessionId);
            var phantomEncryptionPubKey = Encoders.Base58.EncodeData(_phantomEncryptionPubKey);
            PlayerPrefs.SetString(PhantomEncryptionPubKeyPrefEntry, phantomEncryptionPubKey);
            PlayerPrefs.SetString(PhantomPublicKeyPrefEntry, account);
        }
        
        private void LoadSessionInfo(PublicKey account)
        {
            var password = DeriveEncryptionPassword(account);
            var keystoreService = new KeyStorePbkdf2Service();
            var encryptedKeystoreJson = PlayerPrefs.GetString(TempKpPrefEntry);
            var decryptedKeystore = keystoreService.DecryptKeyStoreFromJson(password, encryptedKeystoreJson);
            var secret = Encoding.UTF8.GetString(decryptedKeystore);
            _tmpPhantomConnectionAccount = FromSecretKey(secret);
            _sessionId = PlayerPrefs.GetString(SessionIdPrefEntry);
            var phantomEncryptionPubKey = PlayerPrefs.GetString(PhantomEncryptionPubKeyPrefEntry);
            _phantomEncryptionPubKey = Encoders.Base58.DecodeData(phantomEncryptionPubKey);
        }
        
        private void DestroySessionInfo()
        {
            _tmpPhantomConnectionAccount = new();
            _sessionId = null;
            _phantomEncryptionPubKey = null;
            PlayerPrefs.DeleteKey(TempKpPrefEntry);
            PlayerPrefs.DeleteKey(SessionIdPrefEntry);
            PlayerPrefs.DeleteKey(PhantomEncryptionPubKeyPrefEntry);
            PlayerPrefs.DeleteKey(PhantomPublicKeyPrefEntry);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Deterministic password derivation, each account has a unique password
        /// </summary>
        /// <returns></returns>
        private string DeriveEncryptionPassword([NotNull] PublicKey account){
            if (account == null) throw new ArgumentNullException(nameof(account));
            var rawData = account.Key + _deepLinksWalletOptions.SessionEncryptionPassword + Application.platform;
            using SHA256 sha256Hash = SHA256.Create();
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Encoding.UTF8.GetString(bytes);
        }
        
        /// <summary>
        /// Save an Encrypted Secret in a secure way
        /// </summary>
        /// <param name="password">The password used for the encryption</param>
        /// <param name="secret">The secret</param>
        /// <param name="prefKey">The key used for the player preference</param>
        /// <param name="account">The account public key to associated the secret</param>
        /// <returns></returns>
        private IEnumerator SaveEncryptedSecret(
            string password, 
            string secret, 
            string prefKey,
            [NotNull] PublicKey account)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            yield return new WaitForSeconds(.1f);
            password ??= "";
            
            var keystoreService = new KeyStorePbkdf2Service();
            var stringByteArray = Encoding.UTF8.GetBytes(secret);
            var pbkdf2Params = new Pbkdf2Params()
            {
                Dklen = 32, Count = 10000, Prf = "hmac-sha256"
            };
            var encryptedKeystoreJson = keystoreService.EncryptAndGenerateKeyStoreAsJson(
                password, stringByteArray, account.Key, pbkdf2Params);

            PlayerPrefs.SetString(prefKey, encryptedKeystoreJson);
        }
        
        /// <summary>
        /// Returns an instance of Keypair from a secret key
        /// </summary>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        private static Account FromSecretKey(string secretKey)
        {
            try
            {
                var wallet = new Wallet.Wallet(new PrivateKey(secretKey).KeyBytes, "", SeedMode.Bip39);
                return wallet.Account;
            }catch (ArgumentException)
            {
                return null;
            }

        }

        #endregion
    }
}