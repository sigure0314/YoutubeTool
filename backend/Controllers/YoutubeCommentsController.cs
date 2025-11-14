using Microsoft.AspNetCore.Mvc;
using YoutubeTool.Api.Dtos;
using YoutubeTool.Api.Services;

namespace YoutubeTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class YoutubeCommentsController : ControllerBase
{
    private readonly IYoutubeCommentService _commentService;
    private readonly IApiRequestLogger _requestLogger;

    public YoutubeCommentsController(IYoutubeCommentService commentService, IApiRequestLogger requestLogger)
    {
        _commentService = commentService;
        _requestLogger = requestLogger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(YoutubeCommentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetComments([FromQuery] string videoId, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _commentService.GetTopLevelCommentsAsync(videoId, page, cancellationToken);

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _requestLogger.LogRequestAsync(videoId, result.Page, result.Comments.Count, remoteIp, cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
