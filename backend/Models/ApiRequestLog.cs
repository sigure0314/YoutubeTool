namespace YoutubeTool.Api.Models;

public class ApiRequestLog
{
    public int Id { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public int RequestedPage { get; set; }
    public int ReturnedCount { get; set; }
    public string? RequestIp { get; set; }
}
