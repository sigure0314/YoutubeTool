namespace YoutubeTool.Api.Options;

public class YoutubeApiOptions
{
    public const string ConfigurationSectionName = "YoutubeApi";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.googleapis.com/youtube/v3";
    public int MaxPageSize { get; set; } = 100;
}
