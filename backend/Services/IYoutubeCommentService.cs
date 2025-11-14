using YoutubeTool.Api.Dtos;

namespace YoutubeTool.Api.Services;

public interface IYoutubeCommentService
{
    Task<YoutubeCommentsResponse> GetTopLevelCommentsAsync(
        string videoId,
        int page,
        CancellationToken cancellationToken = default);
}
