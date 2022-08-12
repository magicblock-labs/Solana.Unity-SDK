using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;

namespace Solana.Unity.SDK
{
    public enum RpcCluster
    {
        MainNet = 0,
        DevNet = 1,
        TestNet = 2,
        Custom
    }

    public abstract class WalletBase : MonoBehaviour, IWalletBase
    {
        public RpcCluster rpcCluster = RpcCluster.DevNet;

        private readonly Dictionary<int, Cluster> _rpcClusterMap = new()
        {
            {0, Cluster.MainNet},
            {1, Cluster.DevNet},
            {2, Cluster.TestNet}
        };

        [HideIfEnumValue("rpcCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string customRpc;

        public bool autoConnectOnStartup;
        private IRpcClient _activeRpcClient;

        public IRpcClient ActiveRpcClient
        {
            get => StartConnection();
            private set => _activeRpcClient = value;
        }

        public Account Account { get; private set; }
        public Mnemonic Mnemonic { get; protected set; }

        public virtual void Awake()
        {
            if (autoConnectOnStartup)
            {
                StartConnection();
            }

            Setup();
        }

        /// <inheritdoc />
        public virtual void Setup()
        {
        }

        /// <inheritdoc />
        public async Task<Account> Login(string password = "")
        {
            Account = await _Login(password);
            return Account;
        }

        /// <summary>
        /// Login to the wallet
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        protected abstract Task<Account> _Login(string password = "");

        /// <inheritdoc />
        public void Logout()
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
        public async Task<double> GetBalance(PublicKey publicKey)
        {
            var balance = await ActiveRpcClient.GetBalanceAsync(publicKey);
            if (balance.Result == null)
            {
                return 0;
            }

            return (double) balance.Result.Value / 1000000000;
        }

        /// <inheritdoc />
        public async Task<double> GetBalance()
        {
            return await GetBalance(Account.PublicKey);
        }

        /// <inheritdoc />
        public async Task<RequestResult<string>> Transfer(PublicKey destination, PublicKey tokenMint, ulong amount)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<TokenAccount[]> GetTokenAccounts(PublicKey tokenMint, PublicKey tokenProgramPublicKey)
        {
            var rpc = ActiveRpcClient;
            var result = await
                rpc.GetTokenAccountsByOwnerAsync(
                    Account.PublicKey,
                    null,
                    tokenProgramPublicKey);
            return result.Result?.Value?.ToArray();
        }

        /// <inheritdoc />
        public async Task<TokenAccount[]> GetTokenAccounts()
        {
            return await GetTokenAccounts(null, TokenProgram.ProgramIdKey);
        }

        /// <inheritdoc />
        public abstract Task<byte[]> SignTransaction(Transaction transaction);

        /// <summary>
        /// Sign and send a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public virtual async Task<RequestResult<string>> SignAndSendTransaction(Transaction transaction)
        {
            return await ActiveRpcClient.SendTransactionAsync(await SignTransaction(transaction));
        }

        /// <summary>
        /// Airdrop sol on wallet
        /// </summary>
        /// <param name="amount">Amount of sol</param>
        /// <returns>Amount of sol</returns>
        public async Task<string> RequestAirdrop(ulong amount = 1000000000)
        {
            var result = await ActiveRpcClient.RequestAirdropAsync(Account.PublicKey, amount);
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
                if (_activeRpcClient == null && rpcCluster != RpcCluster.Custom)
                {
                    _activeRpcClient = ClientFactory.GetClient(_rpcClusterMap[(int) rpcCluster]);
                }

                if (_activeRpcClient == null && rpcCluster == RpcCluster.Custom)
                {
                    _activeRpcClient = ClientFactory.GetClient(customRpc);
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