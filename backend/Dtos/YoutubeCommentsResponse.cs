namespace YoutubeTool.Api.Dtos;

public class YoutubeCommentsResponse
{
    public required string VideoId { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required bool HasMore { get; init; }
    public required IReadOnlyCollection<YoutubeCommentDto> Comments { get; init; }
}
