using System;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Solana.Unity.SDK
{
    /// <summary>
    /// Convenience extensions for <see cref="IWalletBase"/>.
    /// </summary>
    public static class WalletBaseExtensions
    {
        /// <summary>
        /// Sign a UTF-8 encoded string message. The string is encoded to bytes
        /// and forwarded to <see cref="IWalletBase.SignMessage(byte[])"/>.
        /// </summary>
        /// <param name="wallet">The wallet to sign with.</param>
        /// <param name="message">The string message to sign.</param>
        /// <returns>The signature bytes.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="wallet"/> or <paramref name="message"/> is null.
        /// </exception>
        public static Task<byte[]> SignMessage(this IWalletBase wallet, string message)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (message == null) throw new ArgumentNullException(nameof(message));
            return wallet.SignMessage(Encoding.UTF8.GetBytes(message));
        }
    }
}
