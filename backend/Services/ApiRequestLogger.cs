using YoutubeTool.Api.Data;
using YoutubeTool.Api.Models;

namespace YoutubeTool.Api.Services;

public class ApiRequestLogger : IApiRequestLogger
{
    private readonly ApplicationDbContext _context;

    public ApiRequestLogger(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogRequestAsync(string videoId, int page, int returnedCount, string? requestIp, CancellationToken cancellationToken = default)
    {
        var entry = new ApiRequestLog
        {
            VideoId = videoId,
            RequestedPage = page,
            ReturnedCount = returnedCount,
            TimestampUtc = DateTime.UtcNow,
            RequestIp = requestIp
        };

        _context.ApiRequestLogs.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
