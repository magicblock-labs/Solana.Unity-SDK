using System.Text;
using System.Threading.Tasks;
using Solana.Unity.KeyStore;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;

namespace Solana.Unity.SDK
{
    public class InGameWallet: WalletBase
    {
        private const string EncryptedKeystoreKey = "EncryptedKeystore";

        protected override Task<Account> _Login(string password = null)
        {
            var keystoreService = new SecretKeyStoreService();
            var encryptedKeystoreJson = LoadPlayerPrefs(EncryptedKeystoreKey);
            var decryptedKeystore = keystoreService.DecryptKeyStoreFromJson(password, encryptedKeystoreJson);
            var mnemonicString = Encoding.UTF8.GetString(decryptedKeystore);
            var restoredMnemonic = new Mnemonic(mnemonicString);
            var wallet = new Wallet.Wallet(restoredMnemonic);
            Mnemonic = restoredMnemonic;
            return Task.FromResult(wallet.GetAccount(0));
        }

        protected override Task<Account> _CreateAccount(string mnemonic = null, string password = null)
        {
            Mnemonic mnem = mnemonic != null ? new Mnemonic(mnemonic): new Mnemonic(WordList.English, WordCount.Twelve);
            var wallet = new Wallet.Wallet(mnem);
            password ??= "";

            var keystoreService = new SecretKeyStoreService();
            var stringByteArray = Encoding.UTF8.GetBytes(mnem.ToString());
            var encryptedKeystoreJson = keystoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password, stringByteArray, wallet.Account.PublicKey.Key);
            
            SavePlayerPrefs(EncryptedKeystoreKey, encryptedKeystoreJson);
            Mnemonic = mnem;
            return Task.FromResult(new Account(
                wallet.GetAccount(0).PrivateKey.KeyBytes,
                wallet.GetAccount(0).PublicKey.KeyBytes));
        }

        public override Task<byte[]> SignTransaction(Transaction transaction)
        {
            transaction.Sign(Account);
            return Task.FromResult(transaction.Serialize());
        }
        
        private string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        
        public void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            #if UNITY_WEBGL
            PlayerPrefs.Save();
            #endif
        }
    }
}