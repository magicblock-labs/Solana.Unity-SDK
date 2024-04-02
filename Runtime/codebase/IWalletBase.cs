using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

namespace Solana.Unity.SDK
{
    /// <summary>
    /// Interface for Solana wallet base functionality
    /// </summary>
    public abstract class WalletBase : IWalletBase
    {
        /// <summary>
        /// Perform wallet setup and initialization, such as wallet state loading and RPC connection
        /// </summary>
        public void Setup() { }

        /// <summary>
        /// Login to the wallet
        /// </summary>
        /// <param name="password">The password used for login, if required</param>
        /// <returns>The account object representing the logged-in wallet</returns>
        public abstract Task<Account> Login(string password = null);

        /// <summary>
        /// Wallet logout
        /// </summary>
        public abstract void Logout();

        /// <summary>
        /// Creates and configures an account with private and public keys, using mnemonics if provided
        /// </summary>
        /// <param name="mnemonic">The mnemonic to use</param>
        /// <param name="password">The password used for encryption if the mnemonic needs to be stored</param>
        /// <returns>The account object representing the created wallet</returns>
        public abstract Task<Account> CreateAccount(string mnemonic = null, string password = null);

        /// <summary>
        /// Get the SOL balance for a Token Account PublicKey
        /// </summary>
        /// <param name="publicKey">The public key of the token account</param>
        /// <param name="commitment">The desired commitment level</param>
        /// <returns>The SOL balance of the specified token account</returns>
        public abstract Task<double> GetBalance(PublicKey publicKey, Commitment commitment);

        /// <summary>
        /// Get the SOL balance
        /// </summary>
        /// <param name="commitment">The desired commitment level</param>
        /// <returns>The SOL balance of the wallet</returns>
        public abstract Task<double> GetBalance(Commitment commitment = Commitment.Confirmed);

        /// <summary>
        /// Transfer a certain amount of a given tokenMint to a destination account 
        /// </summary>
        /// <param name="destination">The destination public key</param>
        /// <param name="tokenMint">The public key of the token mint</param>
        /// <param name="amount">The amount to transfer</param>
        /// <param name="commitment">The desired commitment level</param>
        /// <returns>The result of the transfer operation</returns>
        public abstract Task<RequestResult<string>> Transfer(PublicKey destination, PublicKey tokenMint, ulong amount, Commitment commitment = Commitment.Confirmed);

        /// <summary>
        /// Transfer a certain amount of SOL to a destination account
        /// </summary>
        /// <param name="destination">The destination public key</param>
        /// <param name="amount">The amount of SOL to transfer</param>
        /// <param name="commitment">The desired commitment level</param>
        /// <returns>The result of the transfer operation</returns>
        public abstract Task<RequestResult<string>> Transfer(PublicKey destination, ulong amount, Commitment commitment = Commitment.Confirmed);

        /// <summary>
        /// Returns tokens held by the given publicKey
        /// </summary>
        /// <param name="tokenMint">The public key of the token mint</param>
        /// <param name="tokenProgramPublicKey">The public key of the token program</param>
        /// <returns>Array of token accounts</returns>
        public abstract Task<TokenAccount[]> GetTokenAccounts(PublicKey tokenMint, PublicKey tokenProgramPublicKey);

        /// <summary>
        /// Returns tokens held by the wallet
        /// </summary>
        /// <param name="commitment">The desired commitment level</param>
        /// <returns>Array of token accounts</returns>
        public abstract Task<TokenAccount[]> GetTokenAccounts(Commitment commitment = Commitment.Confirmed);

        /// <summary>
        /// Sign a transaction
        /// </summary>
        /// <param name="transaction">The transaction to sign</param>
        /// <returns>The signed transaction</returns>
        public abstract Task<Transaction> SignTransaction(Transaction transaction);

        /// <summary>
        /// Sign all transactions
        /// </summary>
        /// <param name="transactions">The transactions to sign</param>
        /// <returns>Array of signed transactions</returns>
        public abstract Task<Transaction[]> SignAllTransactions(Transaction[] transactions);

        /// <summary>
        /// Sign a message
        /// </summary>
        /// <param name="message">The message to sign</param>
        /// <returns>The signed message</returns>
        public abstract Task<byte[]> SignMessage(byte[] message);

        /// <summary>
        /// Sign and send a transaction
        /// </summary>
        /// <param name="transaction">The transaction to sign and send</param>
        /// <param name="skipPreflight">Whether to skip preflight checks</param>
        /// <param name="commitment">The desired commitment level</param>
        /// <returns>The result of the transaction sending operation</returns>
        public abstract Task<RequestResult<string>> SignAndSendTransaction(Transaction transaction, bool skipPreflight, Commitment commitment);

        /// <summary>
        /// Check if there are available wallets without performing login
        /// </summary>
        /// <returns>True if there are available wallets, false otherwise</returns>
        public abstract Task<bool> CanLogin();
    }
}
