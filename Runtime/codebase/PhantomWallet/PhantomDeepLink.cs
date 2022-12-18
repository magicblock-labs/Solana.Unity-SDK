using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        
        private TaskCompletionSource<Account> _loginTaskCompletionSource = new();
        private TaskCompletionSource<Transaction> _signedTransactionTaskCompletionSource = new();
        private List<SignaturePubKeyPair> _signatures;
        
        public PhantomDeepLink(
            PhantomWalletOptions phantomWalletOptions,
            RpcCluster rpcCluster = RpcCluster.DevNet, string customRpc = null, bool autoConnectOnStartup = false) 
            : base(rpcCluster, customRpc, autoConnectOnStartup)
        {
            _phantomWalletOptions = phantomWalletOptions;
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeepLinkActivated(Application.absoluteURL);
        }

        protected override Task<Account> _Login(string password = null)
        {
            StartLogin();
            return _loginTaskCompletionSource.Task;
        }
        
        public override void Logout()
        {
            base.Logout();
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }
        
        public override Task<Transaction> SignTransaction(Transaction transaction)
        {
            _signedTransactionTaskCompletionSource = new TaskCompletionSource<Transaction>();
            _signatures = transaction.Signatures;
            if (transaction.Signatures.Count == 0)
            {
                transaction.Signatures.Add(new SignaturePubKeyPair()
                {
                    PublicKey = transaction.FeePayer,
                    Signature = new Byte[64]
                });
            }
            StartSignTransaction(transaction);
            return _signedTransactionTaskCompletionSource.Task;
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            throw new NotImplementedException();
        }
        
        #region DeepLinks

        private void StartLogin()
        {
            var url = Utils.CreateLoginDeepLink(
                redirectScheme: _phantomWalletOptions.deeplinkUrlScheme,
                metadataUrl: _phantomWalletOptions.appMetaDataUrl,
                apiVersion: _phantomWalletOptions.phantomApiVersion,
                connectionPublicKey: Encoders.Base58.EncodeData(PhantomConnectionAccountPublicKey),
                cluster: RpcCluster
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
                cluster: RpcCluster
                
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
        }

        private void ParseConnectionSuccessful(string url)
        {
            var phantomResponse = url.Split("?"[0])[1];
            var result = HttpUtility.ParseQueryString(phantomResponse);
            _phantomEncryptionPubKey = Encoders.Base58.DecodeData(result.Get("phantom_encryption_public_key"));
            var phantomNonce = result.Get("nonce");
            var data = result.Get("data");
            var errorMessage = result.Get("errorMessage");
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
            var phantomResponse = url.Split("?"[0])[1];
            var result = HttpUtility.ParseQueryString(phantomResponse);
            var nonce = result.Get("nonce");
            var data = result.Get("data");
            var errorMessage = result.Get("errorMessage");
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

        #endregion
    }
}