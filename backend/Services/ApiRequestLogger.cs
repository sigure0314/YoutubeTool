using Microsoft.Extensions.Logging;

namespace YoutubeTool.Api.Services;

public class ApiRequestLogger : IApiRequestLogger
{
    private readonly ILogger<ApiRequestLogger> _logger;

    public ApiRequestLogger(ILogger<ApiRequestLogger> logger)
    {
        _logger = logger;
    }

    public Task LogRequestAsync(string videoId, int page, int returnedCount, string? requestIp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "YouTube comments requested for video {VideoId} (page {Page}). Returned {ReturnedCount} unique comments. Request IP: {RequestIp}",
            videoId,
            page,
            returnedCount,
            requestIp);

        return Task.CompletedTask;
    }
}
