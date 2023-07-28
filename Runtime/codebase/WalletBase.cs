using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;
using WebSocketSharp;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public enum RpcCluster
    {
        MainNet = 0,
        DevNet = 1,
        TestNet = 2
    }

    public abstract class WalletBase : IWalletBase
    {
        public const long SolLamports = 1000000000;
        public RpcCluster RpcCluster  { get; }
        
        public int RpcMaxHits = 30;
        public int RpcMaxHitsPerSeconds = 1;

        private readonly Dictionary<int, Cluster> _rpcClusterMap = new ()
        {
            { 0, Cluster.MainNet },
            { 1, Cluster.DevNet },
            { 2, Cluster.TestNet }
        };

        protected readonly Dictionary<int, string> RPCNameMap = new ()
        {
            { 0, "mainnet-beta" },
            { 1, "devnet" },
            { 2, "testnet" },
            { 3, "mainnet-beta" },
        };

        protected readonly string CustomRpcUri;
        protected string CustomStreamingRpcUri;

        private IRpcClient _activeRpcClient;
        public IRpcClient ActiveRpcClient => StartConnection();

        private IStreamingRpcClient _activeStreamingRpcClient;
        private TaskCompletionSource<object> _webSocketConnection;
        public IStreamingRpcClient ActiveStreamingRpcClient => StartStreamingConnection();
        public Account Account { get;protected set; }
        public Mnemonic Mnemonic { get;protected set; }

        protected WalletBase(RpcCluster rpcCluster = RpcCluster.DevNet, string customRpcUri = null, string customStreamingRpcUri = null, bool autoConnectOnStartup = false)
        {
            RpcCluster = rpcCluster;
            CustomRpcUri = customRpcUri;
            CustomStreamingRpcUri = customStreamingRpcUri;
            if (autoConnectOnStartup)
            {
                StartConnection();
            }
            Setup();
        }

        /// <inheritdoc />
        public void Setup() { }

        /// <inheritdoc />
        public async Task<Account> Login(string password = null)
        {
            Account = await _Login(password);
            return Account;
        }

        /// <summary>
        /// Login to the wallet
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        protected abstract Task<Account> _Login(string password = null);

        /// <inheritdoc />
        public virtual void Logout()
        {
            Account = null;
            Mnemonic = null;
        }

        /// <inheritdoc />
        public async Task<Account> CreateAccount(string mnemonic = null, string password = null)
        {
            Account = await _CreateAccount(mnemonic, password);
            return Account;
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected abstract Task<Account> _CreateAccount(string mnemonic = null, string password = null);

        /// <inheritdoc />
        public async Task<double> GetBalance(PublicKey publicKey, Commitment commitment = Commitment.Confirmed)
        {
            var balance= await ActiveRpcClient.GetBalanceAsync(publicKey, commitment);
            return (double)(balance.Result?.Value ?? 0) / SolLamports;
        }

        /// <inheritdoc />
        public async Task<double> GetBalance(Commitment commitment = Commitment.Confirmed)
        {
            return await GetBalance(Account.PublicKey, commitment);
        }

        /// <inheritdoc />
        public async Task<RequestResult<string>> Transfer(
            PublicKey destination,
            PublicKey tokenMint,
            ulong amount,
            Commitment commitment = Commitment.Confirmed)
        {
            var sta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                Account.PublicKey,
                tokenMint);
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(destination, tokenMint);
            var tokenAccounts = await ActiveRpcClient.GetTokenAccountsByOwnerAsync(destination, tokenMint, null);
            var transaction = new Transaction
            {
                RecentBlockHash = await GetBlockHash(),
                FeePayer = Account.PublicKey,
                Instructions = new List<TransactionInstruction>(),
                Signatures = new List<SignaturePubKeyPair>()
            };
            if (tokenAccounts.Result == null || tokenAccounts.Result.Value.Count == 0)
            {
                transaction.Instructions.Add(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                    Account,
                    destination,
                    tokenMint));
            }
            transaction.Instructions.Add(
                TokenProgram.Transfer(
                sta,
                ata,
                amount,
                Account
            ));
            return await SignAndSendTransaction(transaction, commitment: commitment);
        }

        /// <inheritdoc />
        public async Task<RequestResult<string>> Transfer(PublicKey destination, ulong amount,
            Commitment commitment = Commitment.Confirmed)
        {
            var transaction = new Transaction
            {
                RecentBlockHash = await GetBlockHash(),
                FeePayer = Account.PublicKey,
                Instructions = new List<TransactionInstruction>
                {
                    SystemProgram.Transfer(
                        Account.PublicKey,
                        destination,
                        amount)
                },
                Signatures = new List<SignaturePubKeyPair>()
            };
            return await SignAndSendTransaction(transaction, commitment: commitment);
        }

        /// <inheritdoc />
        public async Task<TokenAccount[]> GetTokenAccounts(PublicKey tokenMint, PublicKey tokenProgramPublicKey)
        {
            var rpc = ActiveRpcClient;
            var result = await
                rpc.GetTokenAccountsByOwnerAsync(
                    Account.PublicKey,
                    tokenMint,
                    tokenProgramPublicKey);
            return result.Result?.Value?.ToArray();
        }

        /// <inheritdoc />
        public async Task<TokenAccount[]> GetTokenAccounts(Commitment commitment = Commitment.Confirmed)
        {
            var rpc = ActiveRpcClient;
            var result = await
                rpc.GetTokenAccountsByOwnerAsync(
                    Account.PublicKey,
                    null,
                    TokenProgram.ProgramIdKey,
                    commitment);
            return result.Result?.Value?.ToArray();
        }

        /// <summary>
        /// Sign a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        protected abstract Task<Transaction> _SignTransaction(Transaction transaction);

        /// <inheritdoc />
        public virtual async Task<Transaction> SignTransaction(Transaction transaction)
        {
            transaction.Sign(Account);
            transaction.Signatures = DeduplicateTransactionSignatures(transaction.Signatures, allowEmptySignatures: true);
            var tx = await _SignTransaction(transaction);
            tx.Signatures = DeduplicateTransactionSignatures(tx.Signatures);
            return tx;
        }


        /// <summary>
        /// Sign all transactions
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        protected abstract Task<Transaction[]> _SignAllTransactions(Transaction[] transactions);

        /// <inheritdoc />
        public virtual async Task<Transaction[]> SignAllTransactions(Transaction[] transactions)
        {
            foreach (var transaction in transactions)
            {
                transaction.PartialSign(Account);
                transaction.Signatures = DeduplicateTransactionSignatures(transaction.Signatures, allowEmptySignatures: true);
            }
            Transaction[] signedTxs = await _SignAllTransactions(transactions);
            for (int i = 0; i < signedTxs.Length; i++)
            {
                signedTxs[i].Signatures = DeduplicateTransactionSignatures(signedTxs[i].Signatures);
            }
            return signedTxs;
        }

        /// <inheritdoc />
        public virtual async Task<RequestResult<string>> SignAndSendTransaction
        (
            Transaction transaction,
            bool skipPreflight = false,
            Commitment commitment = Commitment.Confirmed)
        {
            var signedTransaction = await SignTransaction(transaction);
            return await ActiveRpcClient.SendTransactionAsync(
                Convert.ToBase64String(signedTransaction.Serialize()),
                skipPreflight: skipPreflight, preFlightCommitment: commitment);
        }

        /// <inheritdoc />
        public abstract Task<byte[]> SignMessage(byte[] message);

        /// <summary>
        /// Airdrop sol on wallet
        /// </summary>
        /// <param name="amount">Amount of sol</param>
        /// <param name="commitment"></param>
        /// <returns>Amount of sol</returns>
        public async Task<RequestResult<string>> RequestAirdrop(ulong amount = SolLamports, Commitment commitment = Commitment.Confirmed)
        {
            return await ActiveRpcClient.RequestAirdropAsync(Account.PublicKey, amount, commitment); ;
        }

        #region helpers

        private readonly IDictionary<string, (DateTime, string)> _commitmentCache = new Dictionary<string, (DateTime, string)>();

        /// <summary>
        /// Get the Recent blockhash
        /// </summary>
        /// <param name="commitment"></param>
        /// <param name="useCache"></param>
        /// <param name="maxSeconds"></param>
        /// <returns></returns>
        public async Task<string> GetBlockHash(
            Commitment commitment = Commitment.Confirmed,
            bool useCache = true,
            int maxSeconds = 0)
        {
            if(ActiveRpcClient == null) return null;
            var exists = _commitmentCache.TryGetValue(commitment.ToString(), out var cacheEntry);
            if (useCache && maxSeconds > 0)
            {
                switch (exists)
                {
                    case true when (DateTime.Now - cacheEntry.Item1).TotalSeconds < maxSeconds:
                        return cacheEntry.Item2;
                    case true:
                        _commitmentCache.Remove(commitment.ToString());
                        exists = false;
                        break;
                }
            }
            var blockhash = (await ActiveRpcClient.GetLatestBlockHashAsync(commitment)).Result?.Value?.Blockhash;
            if(exists) _commitmentCache.Remove(commitment.ToString());
            if(blockhash != null && useCache)_commitmentCache.Add(commitment.ToString(), (DateTime.Now, blockhash));
            return blockhash;
        }

        #endregion



        /// <summary>
        /// Start RPC connection and return new RPC Client
        /// </summary>
        /// <returns></returns>
        private IRpcClient StartConnection()
        {
            try
            {
                if (_activeRpcClient == null && CustomRpcUri.IsNullOrEmpty())
                {
                    _activeRpcClient = ClientFactory.GetClient(
                        _rpcClusterMap[(int)RpcCluster], 
                        null, 
                        rateLimiter: UnityRateLimiter.Create().AllowHits(RpcMaxHits).PerSeconds(RpcMaxHitsPerSeconds)
                    );
                }
                if (_activeRpcClient == null && !CustomRpcUri.IsNullOrEmpty())
                {
                    _activeRpcClient = ClientFactory.GetClient(
                        CustomRpcUri,
                        null,
                        rateLimiter: UnityRateLimiter.Create().AllowHits(RpcMaxHits).PerSeconds(RpcMaxHitsPerSeconds));
                }

                return _activeRpcClient;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Start streaming RPC connection and return a new streaming RPC Client
        /// </summary>
        /// <returns></returns>
        private IStreamingRpcClient StartStreamingConnection()
        {
            if (_activeStreamingRpcClient == null && CustomStreamingRpcUri.IsNullOrEmpty())
            {
                CustomStreamingRpcUri = ActiveRpcClient.NodeAddress.AbsoluteUri.Replace("https://", "wss://");
            }
            try
            {
                if (_activeStreamingRpcClient != null) return _activeStreamingRpcClient;
                if (CustomStreamingRpcUri != null)
                {
                    _webSocketConnection = new TaskCompletionSource<object>();
                    _activeStreamingRpcClient = ClientFactory.GetStreamingClient(CustomStreamingRpcUri);
                    _activeStreamingRpcClient
                        .ConnectAsync()
                        .AsUniTask()
                        .ContinueWith(() =>
                        {
                            _webSocketConnection.TrySetResult(null);
                            Debug.Log("WebSockets connection: " + _activeStreamingRpcClient.State);
                        })
                        .Forget();
                    return _activeStreamingRpcClient;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task AwaitWsRpcConnection()
        {
            var wsConnection = ActiveStreamingRpcClient;
            if(wsConnection?.State.Equals(WebSocketState.Open) ?? false) return Task.CompletedTask;
            return _webSocketConnection.Task;
        }


        /// <summary>
        /// Deduplicate transaction signatures, remove empty signatures if allowEmptySignatures is false
        /// </summary>
        /// <param name="signatures"></param>
        /// <param name="allowEmptySignatures"></param>
        /// <returns></returns>
        private static List<SignaturePubKeyPair> DeduplicateTransactionSignatures(
            List<SignaturePubKeyPair> signatures, bool allowEmptySignatures = false)
        {
            var signaturesList = new List<SignaturePubKeyPair>();
            var signaturesSet = new HashSet<PublicKey>();
            var emptySgn = new byte[64];
            foreach (var sgn in signatures)
            {
                if (sgn.Signature.SequenceEqual(emptySgn) && !allowEmptySignatures)
                {
                    var notEmptySig = signatures.FirstOrDefault(
                        s => s.PublicKey.Equals(sgn.PublicKey) && !s.Signature.SequenceEqual(emptySgn));
                    if (notEmptySig != null && !signaturesSet.Contains(notEmptySig.PublicKey))
                    {
                        signaturesSet.Add(notEmptySig.PublicKey);
                        signaturesList.Add(notEmptySig);
                    }
                }
                if ((sgn.Signature.SequenceEqual(emptySgn) && !allowEmptySignatures) || signaturesSet.Contains(sgn.PublicKey)) continue;
                signaturesSet.Add(sgn.PublicKey);
                signaturesList.Add(sgn);
            }
            return signaturesList;
        }
    }
}
