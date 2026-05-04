using System.Threading;
using System.Threading.Tasks;

namespace Solana.Unity.SolanaMobileStack
{
    public interface IMobileWalletAdapterSession
    {
        Task<TResponse> InvokeAsync<TResponse>(string method, object @params, CancellationToken ct);

        Task StartAsync(CancellationToken ct);

        Task CloseAsync();
    }
}
