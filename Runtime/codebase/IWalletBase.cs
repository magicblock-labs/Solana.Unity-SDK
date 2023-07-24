using System.Threading.Tasks;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    public interface IWalletBase
    {
        /// <summary>
        /// Perform wallet setup and initialization, such as wallet state loading and RPC connection
        /// </summary>
        void Setup();
        
        /// <summary>
        /// login to the wallet
        /// </summary>
        Task<Account> Login(string password=null);
        
        
        /// <summary>
        /// Wallet logout
        /// </summary>
        void Logout();
        
        
        /// <summary>
        /// Creates and configure an account with private and public key, using mnemonics if provided
        /// </summary>
        /// <param name="mnemonic">The mnemonic to use</param>
        /// <param name="password">The password used for encryption if the mnemonic need to be stored</param>
        /// <returns></returns>
        Task<Account> CreateAccount(string mnemonic=null, string password=null);


        /// <summary>
        /// Get the SOL balance for a Token Account PublicKey
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="commitment"></param>
        /// <returns></returns>
        Task<double> GetBalance(PublicKey publicKey, Commitment commitment);
        
        /// <summary>
        /// Get the SOL balance
        /// </summary>
        /// <returns></returns>
        Task<double> GetBalance(Commitment commitment);

        /// <summary>
        /// Transfer a certain amount of a given tokenMint to destination account 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="tokenMint"></param>
        /// <param name="amount"></param>
        /// <param name="commitment"></param>
        /// <returns></returns>
        Task<RequestResult<string>> Transfer(PublicKey destination, PublicKey tokenMint, ulong amount, Commitment commitment);

        /// <summary>
        /// Transfer a certain amount of lamports to a destination account
        /// </summary>
        /// <param name="destination">Destination PublicKey</param>
        /// <param name="amount">SOL amount</param>
        /// <param name="commitment"></param>
        /// <returns></returns>
        Task<RequestResult<string>> Transfer(PublicKey destination, ulong amount, Commitment commitment);

        /// <summary>
        /// Returns tokens held by the given publicKey
        /// </summary>
        /// <param name="tokenMint"></param>
        /// <param name="tokenProgramPublicKey"></param>
        /// <returns></returns>
        Task<TokenAccount[]> GetTokenAccounts(PublicKey tokenMint, PublicKey tokenProgramPublicKey);
        
        /// <summary>
        /// Returns tokens held by the given publicKey
        /// </summary>
        /// <param name="commitment"></param>
        /// <returns></returns>
        Task<TokenAccount[]> GetTokenAccounts(Commitment commitment = Commitment.Confirmed);

        /// <summary>
        /// Sign a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<Transaction> SignTransaction(Transaction transaction);
        
        /// <summary>
        /// Signs all transactions
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        Task<Transaction[]> SignAllTransactions(Transaction[] transactions);

        /// <summary>
        /// Sign a message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<byte[]> SignMessage(byte[] message);

        /// <summary>
        /// Sign and send a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="skipPreflight"></param>
        /// <param name="commitment"></param>
        /// <returns></returns>
        Task<RequestResult<string>> SignAndSendTransaction(Transaction transaction, bool skipPreflight, Commitment commitment);
    }
}