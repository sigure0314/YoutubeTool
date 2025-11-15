using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using YoutubeTool.Api.Data;
using YoutubeTool.Api.Dtos;
using YoutubeTool.Api.Models;
using YoutubeTool.Api.Options;

namespace YoutubeTool.Api.Services;

public class YoutubeCommentService : IYoutubeCommentService
{
    private const int PageSize = 100;

    private readonly HttpClient _httpClient;
    private readonly YoutubeApiOptions _options;
    private readonly ILogger<YoutubeCommentService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private static readonly SemaphoreSlim InitializationSemaphore = new(1, 1);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> VideoLocks = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isDatabaseInitialized;

    public YoutubeCommentService(
        HttpClient httpClient,
        IOptions<YoutubeApiOptions> options,
        ILogger<YoutubeCommentService> logger,
        ApplicationDbContext dbContext)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _dbContext = dbContext;
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

        await EnsureDatabaseReadyAsync(cancellationToken);

        var normalizedPage = Math.Max(1, page);

        var comments = await FetchAllCommentsFromApiAsync(videoId, cancellationToken);

        try
        {
            return await BuildResponseAsync(videoId, normalizedPage, comments, cancellationToken);
        }
        catch (SqliteException ex) when (IsMissingTableException(ex))
        {
            _logger.LogWarning(ex, "Database schema is missing. Re-applying migrations and retrying the request.");

            Volatile.Write(ref _isDatabaseInitialized, false);
            await EnsureDatabaseReadyAsync(cancellationToken);

            return await BuildResponseAsync(videoId, normalizedPage, comments, cancellationToken);
        }
    }

    private async Task<YoutubeCommentsResponse> BuildResponseAsync(
        string videoId,
        int page,
        List<YoutubeComment> comments,
        CancellationToken cancellationToken)
    {
        await PersistCommentsAsync(videoId, comments, cancellationToken);
        var (pageComments, hasMore) = await LoadPageFromDatabaseAsync(videoId, page, cancellationToken);

        return new YoutubeCommentsResponse
        {
            VideoId = videoId,
            Page = page,
            PageSize = PageSize,
            HasMore = hasMore,
            Comments = pageComments
        };
    }

    private async Task<(IReadOnlyCollection<YoutubeCommentDto> Comments, bool HasMore)> LoadPageFromDatabaseAsync(
        string videoId,
        int page,
        CancellationToken cancellationToken)
    {
        var skip = (page - 1) * PageSize;
        var seenAuthors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentPage = new List<YoutubeCommentDto>();
        var uniqueCount = 0;
        var hasMore = false;

        await foreach (var comment in _dbContext.YoutubeComments
            .AsNoTracking()
            .Where(c => c.VideoId == videoId)
            .OrderByDescending(c => c.PublishedAt)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            var authorKey = comment.AuthorChannelId ?? comment.AuthorDisplayName;
            if (string.IsNullOrWhiteSpace(authorKey) || !seenAuthors.Add(authorKey))
            {
                continue;
            }

            uniqueCount++;

            if (uniqueCount <= skip)
            {
                continue;
            }

            if (currentPage.Count < PageSize)
            {
                currentPage.Add(new YoutubeCommentDto(
                    comment.AuthorDisplayName,
                    comment.AuthorChannelUrl,
                    Truncate(comment.CommentText, 50),
                    EnsureUtc(comment.PublishedAt)));
                continue;
            }

            hasMore = true;
            break;
        }

        return (currentPage, hasMore);
    }

    private async Task EnsureDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        if (_isDatabaseInitialized)
        {
            return;
        }

        await InitializationSemaphore.WaitAsync(cancellationToken);

        try
        {
            if (_isDatabaseInitialized)
            {
                return;
            }

            await _dbContext.Database.MigrateAsync(cancellationToken);
            _isDatabaseInitialized = true;
        }
        finally
        {
            InitializationSemaphore.Release();
        }
    }

    private async Task PersistCommentsAsync(string videoId, List<YoutubeComment> comments, CancellationToken cancellationToken)
    {
        var videoLock = VideoLocks.GetOrAdd(videoId, _ => new SemaphoreSlim(1, 1));
        await videoLock.WaitAsync(cancellationToken);

        try
        {
            await ExecuteWithRetryAsync(async retryCancellationToken =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(retryCancellationToken);

                await _dbContext.YoutubeComments
                    .Where(c => c.VideoId == videoId)
                    .ExecuteDeleteAsync(retryCancellationToken);

                var uniqueComments = DeduplicateComments(comments);

                if (uniqueComments.Count > 0)
                {
                    await _dbContext.YoutubeComments.AddRangeAsync(uniqueComments, retryCancellationToken);
                    await _dbContext.SaveChangesAsync(retryCancellationToken);
                }

                await transaction.CommitAsync(retryCancellationToken);
            }, cancellationToken);
        }
        finally
        {
            videoLock.Release();

            if (videoLock.CurrentCount == 1)
            {
                VideoLocks.TryRemove(videoId, out _);
            }
        }
    }

    private static List<YoutubeComment> DeduplicateComments(IEnumerable<YoutubeComment> comments)
    {
        var uniqueComments = new Dictionary<(string VideoId, string CommentId), YoutubeComment>();

        foreach (var comment in comments)
        {
            var key = (comment.VideoId, comment.CommentId);

            if (!uniqueComments.TryGetValue(key, out var existing))
            {
                uniqueComments[key] = comment;
                continue;
            }

            if (comment.PublishedAt > existing.PublishedAt)
            {
                uniqueComments[key] = comment;
            }
        }

        return uniqueComments.Values.ToList();
    }

    private async Task ExecuteWithRetryAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        var delays = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(500)
        };

        Exception? lastException = null;

        foreach (var delay in delays)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (SqliteException ex) when (IsTransientSqliteException(ex))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Transient SQLite error while persisting comments. Retrying...");
                _dbContext.ChangeTracker.Clear();
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        throw new InvalidOperationException("The retry operation completed without executing.");
    }

    private static bool IsTransientSqliteException(SqliteException exception)
    {
        return exception.SqliteErrorCode is 5 or 6 or 262;
    }

    private static bool IsMissingTableException(SqliteException exception)
    {
        return exception.SqliteErrorCode == 1 &&
               exception.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<YoutubeComment>> FetchAllCommentsFromApiAsync(string videoId, CancellationToken cancellationToken)
    {
        var collectedComments = new List<YoutubeComment>();
        string? nextPageToken = null;
        var retrievalTime = DateTime.UtcNow;

        do
        {
            var (pageComments, token) = await FetchCommentsPageAsync(videoId, nextPageToken, retrievalTime, cancellationToken);
            collectedComments.AddRange(pageComments);
            nextPageToken = token;
        }
        while (!string.IsNullOrEmpty(nextPageToken));

        return collectedComments;
    }

    private async Task<(List<YoutubeComment> Comments, string? NextPageToken)> FetchCommentsPageAsync(
        string videoId,
        string? pageToken,
        DateTime retrievalTime,
        CancellationToken cancellationToken)
    {
        var requestUrl = BuildRequestUrl(videoId, pageToken);

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

            var comments = new List<YoutubeComment>();

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

                    var commentId = topLevelCommentElement.TryGetProperty("id", out var commentIdElement)
                        ? commentIdElement.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(commentId))
                    {
                        commentId = Guid.NewGuid().ToString("N");
                    }

                    var authorChannelId = ExtractAuthorChannelId(commentSnippet);
                    var authorDisplayName = commentSnippet.GetProperty("authorDisplayName").GetString() ?? "Unknown";
                    var authorChannelUrl = ExtractAuthorChannelUrl(commentSnippet, authorChannelId);

                    var text = commentSnippet.TryGetProperty("textDisplay", out var textElement)
                        ? textElement.GetString() ?? string.Empty
                        : string.Empty;

                    var sanitizedText = SanitizeComment(text);

                    var publishedAtRaw = commentSnippet.TryGetProperty("publishedAt", out var publishedElement)
                        ? publishedElement.GetDateTime()
                        : DateTime.UtcNow;

                    comments.Add(new YoutubeComment
                    {
                        VideoId = videoId,
                        CommentId = commentId,
                        AuthorChannelId = authorChannelId,
                        AuthorDisplayName = authorDisplayName,
                        AuthorChannelUrl = authorChannelUrl,
                        CommentText = sanitizedText,
                        PublishedAt = EnsureUtc(publishedAtRaw),
                        RetrievedAt = retrievalTime
                    });
                }
            }

            var nextPageToken = document.RootElement.TryGetProperty("nextPageToken", out var tokenElement)
                ? tokenElement.GetString()
                : null;

            return (comments, nextPageToken);
        }
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

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
