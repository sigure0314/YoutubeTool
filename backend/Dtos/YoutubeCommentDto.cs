namespace YoutubeTool.Api.Dtos;

public record YoutubeCommentDto(
    string AuthorDisplayName,
    string AuthorChannelUrl,
    string CommentText,
    DateTime PublishedAt
);
