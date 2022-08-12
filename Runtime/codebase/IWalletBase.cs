using System.Threading.Tasks;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

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
        /// <returns></returns>
        Task<double> GetBalance(PublicKey publicKey);
        
        /// <summary>
        /// Get the SOL balance
        /// </summary>
        /// <returns></returns>
        Task<double> GetBalance();
        
        /// <summary>
        /// Transfer a certain amount of a given tokenMint to destination account 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="tokenMint"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        Task<RequestResult<string>> Transfer(PublicKey destination, PublicKey tokenMint, ulong amount);

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
        /// <returns></returns>
        Task<TokenAccount[]> GetTokenAccounts();

        /// <summary>
        /// Sign a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<byte[]> SignTransaction(Transaction transaction);
    }
}