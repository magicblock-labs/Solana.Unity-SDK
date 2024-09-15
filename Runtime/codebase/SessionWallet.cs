using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Programs;
using Solana.Unity.SessionKeys.GplSession.Accounts;
using Solana.Unity.SessionKeys.GplSession.Program;
using Solana.Unity.Rpc.Types;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public class SessionWallet : InGameWallet
    {
        public PublicKey TargetProgram { get; protected set; }
        public PublicKey SessionTokenPDA { get; protected set; }

        public static SessionWallet Instance;
        private static WalletBase _externalWallet;

        private SessionWallet(RpcCluster rpcCluster = RpcCluster.DevNet,
            string customRpcUri = null, string customStreamingRpcUri = null,
            bool autoConnectOnStartup = false) : base(rpcCluster, customRpcUri, customStreamingRpcUri, autoConnectOnStartup)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new Exception("SessionWallet already exists");
            }
        }

        /// <summary>
        /// Checks if a session wallet exists by checking if the encrypted keystore key is present in the player preferences.
        /// </summary>
        /// <returns>True if a session wallet exists, false otherwise.</returns>
        public bool HasSessionWallet()
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
        
        public void SignInitSessionTx(Transaction tx)
        {
            tx.PartialSign(new[] { _externalWallet.Account, Account });
        }
        
        
        /// <summary>
        /// Creates a new SessionWallet instance and logs in with the provided password if a session wallet exists, otherwise creates a new account and logs in.
        /// </summary>
        /// <param name="targetProgram">The target program to interact with.</param>
        /// <param name="password">The password to decrypt the session keystore.</param>
        /// <param name="externalWallet">The external wallet</param>
        /// <returns>A SessionWallet instance.</returns>
        public static async Task<SessionWallet> GetSessionWallet(PublicKey targetProgram, string password, WalletBase externalWallet = null)
        {
            if(Instance != null) return Instance;
            externalWallet ??= Web3.Wallet;
            _externalWallet = externalWallet;
            SessionWallet sessionWallet = new SessionWallet(externalWallet.RpcCluster, externalWallet.ActiveRpcClient.NodeAddress.ToString());
            sessionWallet.TargetProgram = targetProgram;
            sessionWallet.EncryptedKeystoreKey = $"{_externalWallet.Account.PublicKey}_SessionKeyStore";
            var derivedPassword = DeriveSessionPassword(password);

            if (sessionWallet.HasSessionWallet())
            {
                Debug.Log("Found Session Wallet");
                sessionWallet.Account = await sessionWallet.Login(derivedPassword);
                if (sessionWallet.Account == null)
                {
                    Debug.Log("Session Token is corrupted, deleting and creating a new one");
                    sessionWallet.DeleteSessionWallet();
                    sessionWallet.Logout();
                    Instance = null;
                    return await GetSessionWallet(targetProgram, password, externalWallet);
                }

                sessionWallet.SessionTokenPDA = FindSessionToken(targetProgram, sessionWallet.Account, _externalWallet.Account);

                Debug.Log(sessionWallet.SessionTokenPDA);

                if (!await sessionWallet.IsSessionTokenInitialized())
                {
                    Debug.Log("Session Token is not initialized");
                    return sessionWallet;
                }

                if (await sessionWallet.IsSessionTokenValid())
                {
                    Debug.Log("Session Token is valid");
                    return sessionWallet;
                }

                Debug.Log("Session Token is invalid");
                await sessionWallet.CloseSession();
                sessionWallet.Logout();
                Instance = null;
                return await GetSessionWallet(targetProgram, password, externalWallet);
            }

            sessionWallet.Account = await sessionWallet.CreateAccount(password: derivedPassword);
            sessionWallet.SessionTokenPDA = FindSessionToken(targetProgram, sessionWallet.Account, _externalWallet.Account);
            return sessionWallet;
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
                Authority = _externalWallet.Account,
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
                Authority = _externalWallet.Account,
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
        public async Task<bool> IsSessionTokenInitialized(Commitment commitment = Commitment.Confirmed)
        {
            var sessionTokenData = await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA, commitment);
            return sessionTokenData?.Result?.Value != null;
        }

        /// <summary>
        /// Checks if the session token is still valid by verifying if the session token account exists on the blockchain and if its validity period has not expired.
        /// </summary>
        /// <returns>True if the session token is still valid, false otherwise.</returns>
        public async Task<bool> IsSessionTokenValid(Commitment commitment = Commitment.Confirmed)
        {
            var sessionTokenDataResult = await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA, commitment);
            var sessionTokenData = sessionTokenDataResult.Result?.Value?.Data?[0];
            if (sessionTokenData == null) return false;
            return SessionToken.Deserialize(Convert.FromBase64String(sessionTokenData)).ValidUntil > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Returns the authority of the session token account.
        /// </summary>
        /// <param name="commitment"></param>
        /// <returns></returns>
        public async Task<PublicKey> Authority(Commitment commitment = Commitment.Confirmed)
        {
            var sessionTokenDataResult = await ActiveRpcClient.GetAccountInfoAsync(SessionTokenPDA, commitment);
            var sessionTokenData = sessionTokenDataResult.Result?.Value?.Data?[0];
            if (sessionTokenData == null) return null;
            return SessionToken.Deserialize(Convert.FromBase64String(sessionTokenData)).Authority;
        }

        private static string DeriveSessionPassword(string password) {
            var rawData = _externalWallet.Account.PublicKey.Key + password + Application.platform;
            using SHA256 sha256Hash = SHA256.Create();
            // ComputeHash - returns byte array
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Encoding.UTF8.GetString(bytes);
        }


        private void DeleteSessionWallet()
        {
            // Purge Keystore
            PlayerPrefs.DeleteKey(EncryptedKeystoreKey);
            PlayerPrefs.Save();
        }


        /// <summary>
        /// Prepares the session wallet for logout by revoking the session, issuing a refund, and purging the keystore.
        /// NOTE: You must call PrepareLogout before calling Logout to ensure that the session token account is revoked and the refund is issued.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CloseSession(Commitment commitment = Commitment.Confirmed)
        {
            Debug.Log("Preparing Logout");
            // Revoke Session
            var tx = new Transaction()
            {
                FeePayer = Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await GetBlockHash(commitment)
            };

            // Get balance and calculate refund
            var balance = (await GetBalance(Account.PublicKey)) * SolLamports;
            var estimatedFees = await ActiveRpcClient.GetFeeForMessageAsync(tx.CompileMessage(), Commitment.Confirmed);
            var refund = balance - estimatedFees.Result.Value * 1;
            Debug.Log($"LAMPORTS Balance: {balance}, Refund: {refund}");

            tx.Add(RevokeSessionIX());
            // Issue Refund
            if (refund != null)
                tx.Add(SystemProgram.Transfer(Account.PublicKey, _externalWallet.Account.PublicKey, (ulong)refund));
            var rest = await SignAndSendTransaction(tx, commitment: commitment);
            DeleteSessionWallet();
        }
    }
}
