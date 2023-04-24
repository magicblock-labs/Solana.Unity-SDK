using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chaos.NaCl;
using Merkator.Tools;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class PhantomDeepLink: WalletBase
    {
        private readonly PhantomWalletOptions _phantomWalletOptions;
        
        private static readonly Account TmpPhantomConnectionAccount = new();
        private static byte[] PhantomConnectionAccountPrivateKey 
            => ArrayHelpers.SubArray(TmpPhantomConnectionAccount.PrivateKey.KeyBytes, 0, 32);
        private static byte[] PhantomConnectionAccountPublicKey =>
            MontgomeryCurve25519.GetPublicKey(PhantomConnectionAccountPrivateKey);
        
        private string _sessionId;
        private byte[] _phantomEncryptionPubKey;
        
        private TaskCompletionSource<Account> _loginTaskCompletionSource;
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource;
        private TaskCompletionSource<byte[]> _signedMessageTaskCompletionSource;

        public PhantomDeepLink(
            PhantomWalletOptions phantomWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            _phantomWalletOptions = phantomWalletOptions;
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeepLinkActivated(Application.absoluteURL);
        }

        protected override Task<Account> _Login(string password = null)
        {
            _loginTaskCompletionSource = new TaskCompletionSource<Account>();
            StartLogin();
            return _loginTaskCompletionSource.Task;
        }
        
        public override void Logout()
        {
            base.Logout();
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }

        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            StartSignTransaction(transaction);
            return _signedTransactionTaskCompletionSource.Task;
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            throw new NotImplementedException();
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
                _ => RpcCluster.MainNet
            };
        }
        
        #region DeepLinks

        private void StartLogin()
        {
            var url = Utils.CreateLoginDeepLink(
                redirectScheme: _phantomWalletOptions.deeplinkUrlScheme,
                metadataUrl: _phantomWalletOptions.appMetaDataUrl,
                apiVersion: _phantomWalletOptions.phantomApiVersion,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
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
                redirectScheme:  _phantomWalletOptions.deeplinkUrlScheme,
                apiVersion: _phantomWalletOptions.phantomApiVersion,
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
                redirectScheme:  _phantomWalletOptions.deeplinkUrlScheme,
                apiVersion: _phantomWalletOptions.phantomApiVersion,
                sessionId: _sessionId,
                cluster: GetCluster()
                
            );
            Application.OpenURL(url);
        }
        
        #endregion        

        #region Callbacks
        
        private void OnDeepLinkActivated(string url)
        {
            if (url.Contains("transactionSigned"))
            {
                ParseSuccessfullySignedTransaction(url);
            }
            else if(url.Contains("onPhantomConnected"))
            {
                ParseConnectionSuccessful(url);
            }
            else if(url.Contains("messageSigned"))
            {
                ParseSuccessfullySignedMessage(url);
            }
        }

        private void ParseConnectionSuccessful(string url)
        {
            var result = ParseQueryString(url);
            _phantomEncryptionPubKey = Encoders.Base58.DecodeData(result["phantom_encryption_public_key"]);
            result.TryGetValue("nonce", out var phantomNonce);
            result.TryGetValue("data", out var data);
            result.TryGetValue("errorMessage", out var errorMessage);
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
            var k = MontgomeryCurve25519.KeyExchange(_phantomEncryptionPubKey, PhantomConnectionAccountPrivateKey);
            var unencryptedMessage = XSalsa20Poly1305.TryDecrypt(
                Encoders.Base58.DecodeData(data), k, Encoders.Base58.DecodeData(phantomNonce));
            var bytesToUtf8String = Encoding.UTF8.GetString(unencryptedMessage);
            var connectSuccess = JsonUtility.FromJson<PhantomWalletConnectSuccess>(bytesToUtf8String);
            var error = JsonUtility.FromJson<PhantomWalletError>(bytesToUtf8String);
            if (!string.IsNullOrEmpty(connectSuccess.public_key))
            {
                _sessionId = connectSuccess.session;
                _loginTaskCompletionSource.SetResult(new Account(string.Empty, connectSuccess.public_key));
            }
            else
            {
                if (string.IsNullOrEmpty(error.errorCode)) return;
                _loginTaskCompletionSource.SetResult(null);
                Debug.LogError($"Deeplink error: {error.errorCode} {error.errorMessage}");
            }
        }

        private void ParseSuccessfullySignedTransaction(string url)
        {
            var result = ParseQueryString(url);
            result.TryGetValue("nonce", out var nonce);
            result.TryGetValue("data", out var data);
            result.TryGetValue("errorMessage", out var errorMessage);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"Deeplink error: Error: {errorMessage} + Data: {data}");
                return;
            }
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
            if (!string.IsNullOrEmpty(errorMessage))
            {
                Debug.LogError($"Deeplink error: Error: {errorMessage} + Data: {data}");
                return;
            }
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
    }
}