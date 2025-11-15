using System.Threading;
using System.Threading.Tasks;

namespace YoutubeTool.Api.Services;

public interface IDatabaseInitializer
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    void Reset();
}
