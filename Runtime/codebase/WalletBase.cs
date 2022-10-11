using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public enum RpcCluster
    {
        MainNet = 0,
        DevNet = 1,
        TestNet = 2,
        Custom = 3
    }

    public abstract class WalletBase : IWalletBase
    {
        private const long SolLamports = 1000000000;
        public RpcCluster RpcCluster  { get; }

        private readonly Dictionary<int, Cluster> _rpcClusterMap = new ()
        {
            { 0, Cluster.MainNet },
            { 1, Cluster.DevNet },
            { 2, Cluster.TestNet }
        };

        private readonly string _customRpc;

        private IRpcClient _activeRpcClient;
        public IRpcClient ActiveRpcClient {
            get => StartConnection();
            private set => _activeRpcClient = value; }
        public Account Account { get;private set; }
        public Mnemonic Mnemonic { get;protected set; }

        protected WalletBase(RpcCluster rpcCluster = RpcCluster.DevNet, string customRpc = null, bool autoConnectOnStartup = false)
        {
            RpcCluster = rpcCluster;
            _customRpc = customRpc;
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
            Mnemonic = new Mnemonic(mnemonic);
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
        public async Task<double> GetBalance(PublicKey publicKey, Commitment commitment = Commitment.Finalized)
        {
            var balance= await ActiveRpcClient.GetBalanceAsync(publicKey, commitment);
            return (double)balance.Result.Value / SolLamports;
        }
        
        /// <inheritdoc />
        public async Task<double> GetBalance(Commitment commitment = Commitment.Finalized)
        {
            return await GetBalance(Account.PublicKey, commitment);
        }

        /// <inheritdoc />
        public async Task<RequestResult<string>> Transfer(
            PublicKey destination, 
            PublicKey tokenMint, 
            ulong amount, 
            Commitment commitment = Commitment.Finalized)
        {
            var sta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                Account.PublicKey, 
                tokenMint);
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(destination, tokenMint);
            var tokenAccounts = await ActiveRpcClient.GetTokenAccountsByOwnerAsync(destination, tokenMint, null);
            var blockHash = await ActiveRpcClient.GetRecentBlockHashAsync();
            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
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
            return await SignAndSendTransaction(transaction, commitment);
        }
        
        /// <inheritdoc />
        public async Task<RequestResult<string>> Transfer(PublicKey destination, ulong amount, 
            Commitment commitment = Commitment.Finalized)
        {
            var blockHash = await ActiveRpcClient.GetRecentBlockHashAsync();
            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
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
            return await SignAndSendTransaction(transaction, commitment);
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
        public async Task<TokenAccount[]> GetTokenAccounts()
        {
            var rpc = ActiveRpcClient;
            var result = await rpc.GetTokenAccountsByOwnerAsync(
                Account.PublicKey, 
                null, 
                TokenProgram.ProgramIdKey);
            return result.Result?.Value?.ToArray();
        }

        /// <inheritdoc />
        public abstract Task<Transaction> SignTransaction(Transaction transaction);

        /// <inheritdoc />
        public virtual async Task<RequestResult<string>> SignAndSendTransaction
        (
            Transaction transaction, 
            Commitment commitment = Commitment.Finalized)
        {
            var signedTransaction = await SignTransaction(transaction);
            return await ActiveRpcClient.SendTransactionAsync(
                Convert.ToBase64String(signedTransaction.Serialize()), preFlightCommitment: commitment);
        }

        /// <summary>
        /// Airdrop sol on wallet
        /// </summary>
        /// <param name="amount">Amount of sol</param>
        /// <param name="commitment"></param>
        /// <returns>Amount of sol</returns>
        public async Task<string> RequestAirdrop(ulong amount = SolLamports, Commitment commitment = Commitment.Finalized)
        {
            var result = await ActiveRpcClient.RequestAirdropAsync(Account.PublicKey, amount, commitment);
            return result.Result;
        }
        
        /// <summary>
        /// Start RPC connection and return new RPC Client 
        /// </summary>
        /// <returns></returns>
        private IRpcClient StartConnection()
        {
            try
            {
                if (_activeRpcClient == null && RpcCluster != RpcCluster.Custom)
                {
                    _activeRpcClient = ClientFactory.GetClient(_rpcClusterMap[(int)RpcCluster]);
                }
                if (_activeRpcClient == null && RpcCluster == RpcCluster.Custom)
                {
                    _activeRpcClient = ClientFactory.GetClient(_customRpc);
                }

                return _activeRpcClient;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}