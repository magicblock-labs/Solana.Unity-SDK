using System.Threading.Tasks;

namespace Solana.Unity.SolanaMobileStack
{
    public interface IAuthorizationCache
    {
        Task<AuthorizationRecord> GetAsync();

        Task SetAsync(AuthorizationRecord record);

        Task ClearAsync();
    }
}
