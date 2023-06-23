using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Solana.Unity.KeyStore.Exceptions;
using Solana.Unity.KeyStore.Services;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Programs;
using Solana.Unity.Wallet.Bip39;
using Solana.Unity.Gum.GplSession;
using Solana.Unity.Gum.GplSession.Accounts;
using Solana.Unity.Gum.GplSession.Program;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class SessionWallet : InGameWallet
    {
        private const string EncryptedKeystoreKey = "SessionKeystore";

        public PublicKey TargetProgram { get; protected set; }
        public PublicKey SessionTokenPDA { get; protected set; }

        public SessionWallet(RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null, string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
        }

        public static bool HasSessionWallet()
        {
            var prefs = LoadPlayerPrefs(EncryptedKeystoreKey);
            return !string.IsNullOrEmpty(prefs);
        }

        private PublicKey FindSessionToken() {
            return SessionToken.DeriveSessionTokenAccount(
                authority: Web3.Account,
                targetProgram: TargetProgram,
                sessionSigner: Account.PublicKey
            );
        }

        public static async Task<SessionWallet> GetSessionWallet(PublicKey targetProgram, string password, RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null, string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false)
        {
            SessionWallet sessionWallet = new SessionWallet(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            sessionWallet.TargetProgram = targetProgram;
            if (HasSessionWallet())
            {
                await sessionWallet.Login(password);
            }
            else {
                await sessionWallet.CreateAccount(password);
            }
            sessionWallet.SessionTokenPDA = sessionWallet.FindSessionToken();
            return sessionWallet;
        }

        /// <inheritdoc />
        protected override Task<Account> _Login(string password = "")
        {
            var keystoreService = new KeyStorePbkdf2Service();
            var encryptedKeystoreJson = LoadPlayerPrefs(EncryptedKeystoreKey);
            byte[] decryptedKeystore;
            try
            {
                if (string.IsNullOrEmpty(encryptedKeystoreJson))
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
                secret = mnem.ToString();
            }
            if(account == null) return Task.FromResult<Account>(null);

            password ??= "";

            var keystoreService = new KeyStorePbkdf2Service();
            var stringByteArray = Encoding.UTF8.GetBytes(secret);
            var encryptedKeystoreJson = keystoreService.EncryptAndGenerateKeyStoreAsJson(
                password, stringByteArray, account.PublicKey.Key);

            SavePlayerPrefs(EncryptedKeystoreKey, encryptedKeystoreJson);
            Mnemonic = mnem;
            return Task.FromResult(account);
        }

        public TransactionInstruction CreateSessionIx(bool topUp, long sessionValidity) {
            CreateSessionAccounts createSessionAccounts = new CreateSessionAccounts()
            {
                SessionToken = SessionTokenPDA,
                SessionSigner = Account.PublicKey,
                Authority = Web3.Account,
                TargetProgram = TargetProgram,
                SystemProgram = SystemProgram.ProgramIdKey,
            };

            return GplSessionProgram.CreateSession(
                createSessionAccounts,
                topUp: topUp,
                validUntil: sessionValidity
            );
        }

        public async Task<bool> IsSessionTokenInitialized() {
            var sessionTokenData = await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA);
            return sessionTokenData.Result.Value.Data[0] != null;
        }

        public async Task<bool> IsSessionTokenValid() {
            var sessionTokenData = (await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA)).Result.Value.Data[0];
            if (sessionTokenData == null) return false;
            return SessionToken.Deserialize(Convert.FromBase64String(sessionTokenData)).ValidUntil > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

    }
}
