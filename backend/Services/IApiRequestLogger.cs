namespace YoutubeTool.Api.Services;

public interface IApiRequestLogger
{
    Task LogRequestAsync(string videoId, int page, int returnedCount, string? requestIp, CancellationToken cancellationToken = default);
}
