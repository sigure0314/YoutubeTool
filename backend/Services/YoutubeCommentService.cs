using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using YoutubeTool.Api.Dtos;
using YoutubeTool.Api.Options;

namespace YoutubeTool.Api.Services;

public class YoutubeCommentService : IYoutubeCommentService
{
    private const int PageSize = 40;

    private readonly HttpClient _httpClient;
    private readonly YoutubeApiOptions _options;
    private readonly ILogger<YoutubeCommentService> _logger;

    public YoutubeCommentService(HttpClient httpClient, IOptions<YoutubeApiOptions> options, ILogger<YoutubeCommentService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<YoutubeCommentsResponse> GetTopLevelCommentsAsync(string videoId, int page, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("Video ID is required", nameof(videoId));
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("YouTube API key is not configured.");
        }

        var normalizedPage = Math.Max(1, page);

        var uniqueComments = new List<YoutubeCommentDto>();
        var seenAuthors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? nextPageToken = null;
        bool lastResponseHadMore = false;

        do
        {
            var requestUrl = BuildRequestUrl(videoId, nextPageToken);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "YouTube API request could not be completed.");
                throw new InvalidOperationException("YouTube API request could not be completed.", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("YouTube API request failed with {StatusCode}: {Error}", response.StatusCode, error);
                    throw new InvalidOperationException($"YouTube API request failed with status code {response.StatusCode}.");
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (document.RootElement.TryGetProperty("items", out var itemsElement))
                {
                    foreach (var item in itemsElement.EnumerateArray())
                    {
                        if (!item.TryGetProperty("snippet", out var snippetElement) ||
                            !snippetElement.TryGetProperty("topLevelComment", out var topLevelCommentElement) ||
                            !topLevelCommentElement.TryGetProperty("snippet", out var commentSnippet))
                        {
                            continue;
                        }

                        var authorChannelId = ExtractAuthorChannelId(commentSnippet);
                        var authorKey = authorChannelId ?? commentSnippet.GetProperty("authorDisplayName").GetString() ?? string.Empty;

                        if (string.IsNullOrEmpty(authorKey) || !seenAuthors.Add(authorKey))
                        {
                            continue;
                        }

                        var authorDisplayName = commentSnippet.GetProperty("authorDisplayName").GetString() ?? "Unknown";
                        var authorChannelUrl = ExtractAuthorChannelUrl(commentSnippet, authorChannelId);
                        var publishedAt = commentSnippet.TryGetProperty("publishedAt", out var publishedElement)
                            ? publishedElement.GetDateTime()
                            : DateTime.UtcNow;

                        var text = commentSnippet.TryGetProperty("textDisplay", out var textElement)
                            ? textElement.GetString() ?? string.Empty
                            : string.Empty;

                        var sanitizedText = SanitizeComment(text);

                        uniqueComments.Add(new YoutubeCommentDto(
                            authorDisplayName,
                            authorChannelUrl,
                            Truncate(sanitizedText, 50),
                            publishedAt.ToUniversalTime()));
                    }
                }

                nextPageToken = document.RootElement.TryGetProperty("nextPageToken", out var tokenElement)
                    ? tokenElement.GetString()
                    : null;

                lastResponseHadMore = !string.IsNullOrEmpty(nextPageToken);
            }
        }
        while (uniqueComments.Count < normalizedPage * PageSize && !string.IsNullOrEmpty(nextPageToken));

        var skip = (normalizedPage - 1) * PageSize;
        var pageComments = uniqueComments.Skip(skip).Take(PageSize).ToList();

        var hasMore = uniqueComments.Count > normalizedPage * PageSize || lastResponseHadMore;

        return new YoutubeCommentsResponse
        {
            VideoId = videoId,
            Page = normalizedPage,
            PageSize = PageSize,
            HasMore = hasMore,
            Comments = pageComments
        };
    }

    private string BuildRequestUrl(string videoId, string? pageToken)
    {
        var maxResults = Math.Clamp(_options.MaxPageSize, PageSize, 100);
        var query = new Dictionary<string, string>
        {
            ["part"] = "snippet",
            ["videoId"] = videoId,
            ["maxResults"] = maxResults.ToString(),
            ["textFormat"] = "plainText",
            ["order"] = "time",
            ["key"] = _options.ApiKey
        };

        if (!string.IsNullOrEmpty(pageToken))
        {
            query["pageToken"] = pageToken;
        }

        var queryString = string.Join("&", query.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));
        return $"commentThreads?{queryString}";
    }

    private static string? ExtractAuthorChannelId(JsonElement commentSnippet)
    {
        if (commentSnippet.TryGetProperty("authorChannelId", out var channelIdElement) &&
            channelIdElement.TryGetProperty("value", out var valueElement))
        {
            return valueElement.GetString();
        }

        return null;
    }

    private static string ExtractAuthorChannelUrl(JsonElement commentSnippet, string? authorChannelId)
    {
        if (commentSnippet.TryGetProperty("authorChannelUrl", out var urlElement))
        {
            var url = urlElement.GetString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return !string.IsNullOrWhiteSpace(authorChannelId)
            ? $"https://www.youtube.com/channel/{authorChannelId}"
            : "https://www.youtube.com";
    }

    private static string SanitizeComment(string text)
    {
        var decoded = WebUtility.HtmlDecode(text);
        return Regex.Replace(decoded, "<[^>]+>", string.Empty).Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
