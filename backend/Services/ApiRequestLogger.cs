using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YoutubeTool.Api.Data;
using YoutubeTool.Api.Models;

namespace YoutubeTool.Api.Services;

public class ApiRequestLogger : IApiRequestLogger
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IDatabaseInitializer _databaseInitializer;

    public ApiRequestLogger(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IDatabaseInitializer databaseInitializer)
    {
        _dbContextFactory = dbContextFactory;
        _databaseInitializer = databaseInitializer;
    }

    public async Task LogRequestAsync(string videoId, int page, int returnedCount, string? requestIp, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.EnsureCreatedAsync(cancellationToken);

        var entry = new ApiRequestLog
        {
            VideoId = videoId,
            RequestedPage = page,
            ReturnedCount = returnedCount,
            TimestampUtc = DateTime.UtcNow,
            RequestIp = requestIp
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        context.ApiRequestLogs.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
