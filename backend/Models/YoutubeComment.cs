using System.ComponentModel.DataAnnotations;

namespace YoutubeTool.Api.Models;

public class YoutubeComment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public required string VideoId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string CommentId { get; set; }

    [MaxLength(128)]
    public string? AuthorChannelId { get; set; }

    [Required]
    [MaxLength(256)]
    public required string AuthorDisplayName { get; set; }

    [Required]
    [MaxLength(512)]
    public required string AuthorChannelUrl { get; set; }

    [Required]
    public required string CommentText { get; set; }

    public DateTime PublishedAt { get; set; }

    public DateTime RetrievedAt { get; set; }
}
