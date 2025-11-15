using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YoutubeTool.Api.Data;

namespace YoutubeTool.Api.Services;

public class DatabaseInitializer : IDatabaseInitializer, IAsyncDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private bool _isInitialized;

    public DatabaseInitializer(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _isInitialized))
        {
            return;
        }

        await _initializationSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (_isInitialized)
            {
                return;
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            try
            {
                await context.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply database migrations.");
                throw;
            }

            Volatile.Write(ref _isInitialized, true);
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    public void Reset()
    {
        Volatile.Write(ref _isInitialized, false);
    }

    public ValueTask DisposeAsync()
    {
        _initializationSemaphore.Dispose();
        return ValueTask.CompletedTask;
    }
}
