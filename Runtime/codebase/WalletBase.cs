using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    
    public abstract class WalletBase : MonoBehaviour, WalletBaseInterface
    {
        public RpcCluster rpcCluster = RpcCluster.DevNet;
        private readonly Dictionary<int, Cluster> _rpcClusterMap = new ()
        {
            { 0, Cluster.MainNet },
            { 1, Cluster.DevNet },
            { 2, Cluster.TestNet }
        };
        [DrawIf("rpcCluster", RpcCluster.Custom)]  
        public string customRpc;
        public bool autoConnectOnStartup;
        private IRpcClient _activeRpcClient;
        public IRpcClient ActiveRpcClient {
            get => StartConnection();
            private set => _activeRpcClient = value; }
        public Account Account { get;private set; }
        
        public virtual void Awake()
        {
            if (autoConnectOnStartup)
            {
                StartConnection();
            }
            Setup();
        }

        /// <inheritdoc />
        public void Setup() { }

        /// <inheritdoc />
        public abstract Account Login(string password = null);

        /// <inheritdoc />
        public void Logout()
        {
            Account = null;
        }

        /// <inheritdoc />
        public abstract Account CreateAccount(Mnemonic mnemonic = null, string password = null);
        
        /// <inheritdoc />
        public ulong GetBalance(PublicKey publicKey)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public RequestResult<string> Transfer(PublicKey destination, PublicKey tokenMint, ulong amount)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public Task<TokenAccount[]> GetTokenAccounts(PublicKey publicKey, PublicKey tokenMint, PublicKey tokenProgramPublicKey)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public abstract Transaction SignTransaction(Transaction transaction);

        /// <summary>
        /// Sign and send a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Transaction SignAndSendTransaction(Transaction transaction)
        {
            throw new System.NotImplementedException();
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
                if (ActiveRpcClient == null && rpcCluster != RpcCluster.Custom)
                {
                    ActiveRpcClient = ClientFactory.GetClient(_rpcClusterMap[(int)rpcCluster]);
                }
                if (ActiveRpcClient == null && rpcCluster == RpcCluster.Custom)
                {
                    ActiveRpcClient = ClientFactory.GetClient(customRpc);
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