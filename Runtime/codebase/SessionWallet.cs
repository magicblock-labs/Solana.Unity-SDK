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

        /// <summary>
        /// Checks if a session wallet exists by checking if the encrypted keystore key is present in the player preferences.
        /// </summary>
        /// <returns>True if a session wallet exists, false otherwise.</returns>
        public static bool HasSessionWallet()
        {
            var prefs = LoadPlayerPrefs(EncryptedKeystoreKey);
            return !string.IsNullOrEmpty(prefs);
        }

        /// <summary>
        /// Derives the public key of the session token account for the current session wallet.
        /// </summary>
        /// <returns>The public key of the session token account.</returns>
        private static PublicKey FindSessionToken(PublicKey TargetProgram, Account Account, Account Authority)
        {
            return SessionToken.DeriveSessionTokenAccount(
                authority: Authority.PublicKey,
                targetProgram: TargetProgram,
                sessionSigner: Account.PublicKey
            );
        }

        /// <summary>
        /// Creates a new SessionWallet instance and logs in with the provided password if a session wallet exists, otherwise creates a new account and logs in.
        /// </summary>
        /// <param name="targetProgram">The target program to interact with.</param>
        /// <param name="password">The password to decrypt the session keystore.</param>
        /// <param name="rpcCluster">The Solana RPC cluster to connect to.</param>
        /// <param name="customRpcUri">A custom URI to connect to the Solana RPC cluster.</param>
        /// <param name="customStreamingRpcUri">A custom URI to connect to the Solana streaming RPC cluster.</param>
        /// <param name="autoConnectOnStartup">Whether to automatically connect to the Solana RPC cluster on startup.</param>
        /// <returns>A SessionWallet instance.</returns>
        public static async Task<SessionWallet> GetSessionWallet(PublicKey targetProgram, string password, RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null, string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false)
        {
            SessionWallet sessionWallet = new SessionWallet(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
            sessionWallet.TargetProgram = targetProgram;
            if (HasSessionWallet())
            {
                Debug.Log("Found Session Wallet");
                sessionWallet.Account = await sessionWallet.Login(password);
                sessionWallet.SessionTokenPDA = FindSessionToken(targetProgram, sessionWallet.Account, Web3.Account);

                // If it is not uninitialized, return the session wallet
                if(!(await sessionWallet.IsSessionTokenInitialized())) {
                    Debug.Log("Session Token is not initialized");
                    return sessionWallet;
                }

                // Otherwise check for a valid session token
                if ((await sessionWallet.IsSessionTokenValid())) {
                    Debug.Log("Session Token is valid");
                    return sessionWallet;
                }
                else
                {
                    Debug.Log("Session Token is invalid");
                    await sessionWallet.PrepareLogout();
                    sessionWallet.Logout();
                    return await GetSessionWallet(targetProgram, password, rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup);
                }
            }
            sessionWallet.Account = await sessionWallet.CreateAccount(password:password);
            sessionWallet.SessionTokenPDA = FindSessionToken(targetProgram, sessionWallet.Account, Web3.Account);
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
                if (string.IsNullOrEmpty(encryptedKeystoreJson) || string.IsNullOrEmpty(password))
                    return Task.FromResult<Account>(null);
                decryptedKeystore = keystoreService.DecryptKeyStoreFromJson(password, encryptedKeystoreJson);
            }
            catch (DecryptionException e)
            {
                Debug.LogException(e);
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

        public async Task PrepareLogout(){
            Debug.Log("Preparing Logout");
            // Revoke Session
            var tx = new Transaction()
            {
                FeePayer = Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash()
            };

            // Get balance and calculate refund
            var balance = (await GetBalance(Account.PublicKey)) * SolLamports;
            var estimatedFees = await ActiveRpcClient.GetFeeCalculatorForBlockhashAsync(tx.RecentBlockHash);
            var refund = balance - (estimatedFees.Result.Value.FeeCalculator.LamportsPerSignature * 1);
            Debug.Log($"LAMPORTS Balance: {balance}, Refund: {refund}");

            tx.Add(RevokeSessionIX());
            // Issue Refund
            tx.Add(SystemProgram.Transfer(Account.PublicKey, Web3.Account.PublicKey, (ulong)refund));
            await SignAndSendTransaction(tx);
            // Purge Keystore
            PlayerPrefs.DeleteKey(EncryptedKeystoreKey);
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
            if (account == null) return Task.FromResult<Account>(null);

            password ??= "";

            var keystoreService = new KeyStorePbkdf2Service();
            var stringByteArray = Encoding.UTF8.GetBytes(secret);
            var encryptedKeystoreJson = keystoreService.EncryptAndGenerateKeyStoreAsJson(
                password, stringByteArray, account.PublicKey.Key);

            SavePlayerPrefs(EncryptedKeystoreKey, encryptedKeystoreJson);
            Mnemonic = mnem;
            return Task.FromResult(account);
        }

        /// <summary>
        /// Creates a transaction instruction to create a new session token account and initialize it with the provided session signer and target program.
        /// </summary>
        /// <param name="topUp">Whether to top up the session token account with SOL.</param>
        /// <param name="sessionValidity">The validity period of the session token account, in seconds.</param>
        /// <returns>A transaction instruction to create a new session token account.</returns>
        public TransactionInstruction CreateSessionIX(bool topUp, long sessionValidity)
        {
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

        /// <summary>
        /// Creates a transaction instruction to revoke the current session token account.
        /// </summary>
        /// <returns>A transaction instruction to revoke the current session token account.</returns>
        public TransactionInstruction RevokeSessionIX()
        {
            RevokeSessionAccounts revokeSessionAccounts = new RevokeSessionAccounts()
            {
                SessionToken = SessionTokenPDA,
                // Only the authority of the session token can receive the refund
                Authority = Web3.Account,
                SystemProgram = SystemProgram.ProgramIdKey,
            };

            return GplSessionProgram.RevokeSession(
                revokeSessionAccounts
            );
        }

        /// <summary>
        /// Checks if the session token account has been initialized by checking if the account data is present on the blockchain.
        /// </summary>
        /// <returns>True if the session token account has been initialized, false otherwise.</returns>
        public async Task<bool> IsSessionTokenInitialized()
        {
            var sessionTokenData = await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA);
            return sessionTokenData.Result.Value != null;
        }

        /// <summary>
        /// Checks if the session token is still valid by verifying if the session token account exists on the blockchain and if its validity period has not expired.
        /// </summary>
        /// <returns>True if the session token is still valid, false otherwise.</returns>
        public async Task<bool> IsSessionTokenValid()
        {
            var sessionTokenData = (await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA)).Result.Value.Data[0];
            if (sessionTokenData == null) return false;
            return SessionToken.Deserialize(Convert.FromBase64String(sessionTokenData)).ValidUntil > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

    }
}
