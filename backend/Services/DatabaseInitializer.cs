using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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

    private static readonly string[] RequiredTables =
    {
        "ApiRequestLogs",
        "YoutubeComments"
    };

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

                await EnsureSchemaAsync(context, cancellationToken);
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

    private async Task EnsureSchemaAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                foreach (var table in RequiredTables)
                {
                    var command = $"SELECT 1 FROM \"{table}\" LIMIT 1;";
                    await context.Database.ExecuteSqlRawAsync(command, cancellationToken);
                }

                return;
            }
            catch (SqliteException ex) when (IsMissingTableException(ex) && attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Detected missing SQLite tables. Recreating the database file to restore the schema.");

                await context.Database.EnsureDeletedAsync(cancellationToken);
                await context.Database.MigrateAsync(cancellationToken);
            }
        }

        throw new InvalidOperationException("Database schema validation failed after attempting to recreate the SQLite database.");
    }

    private static bool IsMissingTableException(SqliteException exception)
    {
        return exception.SqliteErrorCode == 1 &&
               exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase);
    }
}
