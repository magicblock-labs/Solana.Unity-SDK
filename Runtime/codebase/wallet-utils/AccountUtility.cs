using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solana.Unity.SDK.Utility
{
    public static class AccountUtility
    {
        public static async Task<AccountInfo> GetAccountData(string accountPublicKey, IRpcClient rpcClient)
        {
            RequestResult<ResponseValue<AccountInfo>> result = await rpcClient.GetAccountInfoAsync(accountPublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value;
            }
            return null;
        }

        public static async Task<AccountInfo> GetAccountData(Account account, IRpcClient rpcClient)
        {
            RequestResult<ResponseValue<AccountInfo>> result = await rpcClient.GetAccountInfoAsync(account.PublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value;
            }
            return null;
        }

        public static async Task<TokenBalance> GetTokenBalance(string tokenPubKey, IRpcClient rpcClient)
        {
            RequestResult<ResponseValue<TokenBalance>> result = await rpcClient.GetTokenAccountBalanceAsync(tokenPubKey);
            if (result.Result != null)
                return result.Result.Value;
            else
                throw new Exception("No balance for this token reveived");
        }

        public static async void CreateAccount(Account account, IRpcClient rpcClient, string toPublicKey = "", ulong ammount = 1000)
        {
            Keypair k = WalletKeyPair.GenerateKeyPairFromMnemonic(WalletKeyPair.GenerateNewMnemonic());
            toPublicKey = k.publicKey;

            RequestResult<ResponseValue<BlockHash>> blockHash = await rpcClient.GetRecentBlockHashAsync();

            var tx = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(SystemProgram.CreateAccount(account.PublicKey, new PublicKey(toPublicKey), ammount, (ulong)TokenProgram.TokenAccountDataSize, SystemProgram.ProgramIdKey)).Build(new List<Account>() { account, new Account(k.privateKeyByte, k.publicKeyByte) });

            RequestResult<string> firstSig = await rpcClient.SendTransactionAsync(Convert.ToBase64String(tx));
        }

        public static async Task<RequestResult<ResponseValue<TokenBalance>>> GetTokenSupply(string key, IRpcClient rpcClient)
        {
            RequestResult<ResponseValue<TokenBalance>> supply = await rpcClient.GetTokenSupplyAsync(key);
            return supply;
        }
    }
}
