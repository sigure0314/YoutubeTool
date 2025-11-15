using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
            var missingTables = await GetMissingTablesAsync(context, cancellationToken);

            if (missingTables.Count == 0)
            {
                return;
            }

            if (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    "Detected missing SQLite tables ({MissingTables}). Recreating the database file to restore the schema.",
                    string.Join(", ", missingTables));

                await context.Database.EnsureDeletedAsync(cancellationToken);
                await context.Database.MigrateAsync(cancellationToken);

                continue;
            }

            throw new InvalidOperationException(
                $"Database schema validation failed after attempting to recreate the SQLite database. Missing tables: {string.Join(", ", missingTables)}");
        }
    }

    private static async Task<IReadOnlyList<string>> GetMissingTablesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var missingTables = new List<string>();
        var connection = context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            foreach (var table in RequiredTables)
            {
                await using var command = CreateTableLookupCommand(connection, table);
                var result = await command.ExecuteScalarAsync(cancellationToken);

                if (result is null || result == DBNull.Value)
                {
                    missingTables.Add(table);
                }
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        return missingTables;
    }

    private static DbCommand CreateTableLookupCommand(DbConnection connection, string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $tableName;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return command;
    }
}
