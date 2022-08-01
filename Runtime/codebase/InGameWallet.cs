using System;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;

namespace Solana.Unity.SDK
{
    public class InGameWallet: WalletBase
    {
        public override Account Login(string password = null)
        {
            throw new NotImplementedException();
        }

        public override Account CreateAccount(Mnemonic mnemonic = null, string password = null)
        {
            if (!WalletKeyPair.CheckMnemonicValidity(mnemonic?.ToString()))
            {
                throw new Exception("Mnemonic is in incorrect format");
            }

            var wallet = new Wallet.Wallet(mnemonic?.ToString(), WordList.AutoDetect(mnemonic?.ToString()));
            
            //string encryptedMnemonics = cypher.Encrypt(mnemonic?.ToString(), password);
            //SavePlayerPrefs(mnemonicsKey, this.mnemonics);
            //SavePlayerPrefs(encryptedMnemonicsKey, encryptedMnemonics);
            return new Account(
                wallet.GetAccount(0).PrivateKey.KeyBytes,
                wallet.GetAccount(0).PublicKey.KeyBytes);
        }

        public override Transaction SignTransaction(Transaction transaction)
        {
            throw new System.NotImplementedException();
        }
    }
}