using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.KeyStore.Exceptions;
using Solana.Unity.KeyStore.Model;
using Solana.Unity.KeyStore.Services;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class InGameWallet : WalletBase
    {
        protected string EncryptedKeystoreKey = "EncryptedKeystore";

        public InGameWallet(RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null, string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
        }

        /// <inheritdoc />
        protected override Task<Account> _Login(string password = "")
        {
            var keystoreService = new KeyStorePbkdf2Service();
            var encryptedKeystoreJson = LoadPlayerPrefs(EncryptedKeystoreKey);
            byte[] decryptedKeystore;
            try
            {
                if (string.IsNullOrEmpty(encryptedKeystoreJson) || string.IsNullOrEmpty(password))
                    return Task.FromResult<Account>(null);
                decryptedKeystore = keystoreService.DecryptKeyStoreFromJson(password, encryptedKeystoreJson);
            }
            catch (DecryptionException)
            {
                return Task.FromResult<Account>(null);
            }

            var secret = Encoding.UTF8.GetString(decryptedKeystore);
            var account = FromSecret(secret);
            if (IsMnemonic(secret))
            {
                var restoredMnemonic = new Mnemonic(secret);
                Mnemonic = restoredMnemonic;
            }
            return Task.FromResult(account);
        }

        /// <inheritdoc />
        protected override Task<Account> _CreateAccount(string secret = null, string password = null)
        {
            Account account;
            Mnemonic mnem = null;
            if (secret != null)
            {
                account = FromSecret(secret);
                if (IsMnemonic(secret))
                {
                    mnem = new Mnemonic(secret);
                }
            }
            else
            {
                mnem = new Mnemonic(WordList.English, WordCount.Twelve);
                var wallet = new Wallet.Wallet(mnem);
                account = wallet.Account;
            }
            if(account == null) return Task.FromResult<Account>(null);
            
            MainThreadDispatcher.Instance().Enqueue(SaveEncryptedAccount(password,
                mnem != null ? mnem.ToString() : secret, account.PublicKey));

            Mnemonic = mnem;
            return Task.FromResult(account);
        }


        private IEnumerator SaveEncryptedAccount(string password, string secret, PublicKey account)
        {
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

            SavePlayerPrefs(EncryptedKeystoreKey, encryptedKeystoreJson);
        }

        /// <inheritdoc />
        protected override Task<Transaction> _SignTransaction(Transaction transaction)
        {
            transaction.Sign(Account);
            return Task.FromResult(transaction);
        }

        protected override Task<Transaction[]> _SignAllTransactions(Transaction[] transactions)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> SignMessage(byte[] message)
        {
            return Task.FromResult(Account.Sign(message));
        }

        /// <summary>
        /// Returns an instance of Keypair from a mnemonic, byte array or secret key
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static Account FromSecret(string secret)
        {
            Account account;
            if (IsMnemonic(secret))
            {
                account = FromMnemonic(secret);
            }
            else if (IsByteArray(secret))
            {
                account = ParseByteArray(secret);
            }
            else
            {
                account = FromSecretKey(secret);
            }

            return account;
        }

        /// <summary>
        /// Returns an instance of Keypair from a mnemonic
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <returns></returns>
        private static Account FromMnemonic(string mnemonic)
        {
            var wallet = new Wallet.Wallet(new Mnemonic(mnemonic));
            return wallet.Account;
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

        /// <summary>
        /// Returns an instance of Keypair from a Byte Array
        /// </summary>
        /// <param name="secretByteArray"></param>
        /// <returns></returns>
        private static Account FromByteArray(byte[] secretByteArray)
        {
            var wallet = new Wallet.Wallet(secretByteArray, "", SeedMode.Bip39);
            return wallet.Account;
        }

        /// <summary>
        /// Takes a string as input and checks if it is a valid mnemonic
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        protected static bool IsMnemonic(string secret)
        {
            return secret.Split(' ').Length is 12 or 24;
        }
        /// <summary>
        /// Takes a string as input and checks if it is a valid byte array
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static bool IsByteArray(string secret)
        {
            return secret.StartsWith('[') && secret.EndsWith(']');
        }

        /// <summary>
        /// Takes a string as input and tries to parse it into a Keypair
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static Account ParseByteArray(string secret)
        {
            var parsed = secret
                .Replace(",]", "]")
                .Trim('[', ']')
                .Split(',')
                .Select(byte.Parse).ToArray();

            return FromByteArray(parsed);
        }


        protected static string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }

        protected static void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            #if UNITY_WEBGL
            PlayerPrefs.Save();
            #endif
        }
    }
}
