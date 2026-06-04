using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK
{
    /// <summary>
    /// Stores the selected Android wallet package used for MWA association.
    /// </summary>
    public interface IMwaWalletSelectionCache
    {
        Task<string> GetSelectedWalletPackage();
        Task SetSelectedWalletPackage(string packageName);
        Task ClearSelectedWalletPackage();
    }
}
