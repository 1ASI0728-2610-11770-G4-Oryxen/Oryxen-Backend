using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Community;
using Oryxen.Application.Community.Contracts;

namespace Oryxen.API.Controllers;

[ApiController]
[Route("api/v1/community")]
[Produces("application/json")]
[Authorize(Roles = $"{Domain.Constants.Roles.Farmer},{Domain.Constants.Roles.Admin}")]
public sealed class CommunityController : ControllerBase
{
    private readonly ICommunityService _communityService;

    public CommunityController(ICommunityService communityService)
    {
        _communityService = communityService;
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var posts = await _communityService.GetFeedAsync(CurrentUserId, page, pageSize, ct);
        return Ok(posts);
    }

    [HttpGet("posts/{id:guid}")]
    public async Task<IActionResult> GetPost(Guid id, CancellationToken ct = default)
    {
        var post = await _communityService.GetPostByIdAsync(id, CurrentUserId, ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost("posts")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> CreatePost(
        [FromForm] string title,
        [FromForm] string content,
        IFormFile? image,
        CancellationToken ct = default)
    {
        var request = new CreatePostRequest { Title = title, Content = content };

        Stream? imageStream = null;
        string? imageFileName = null;

        if (image is not null && image.Length > 0)
        {
            imageStream = image.OpenReadStream();
            imageFileName = image.FileName;
        }

        var post = await _communityService.CreatePostAsync(CurrentUserId, request, imageStream, imageFileName, ct);

        if (imageStream is not null)
            await imageStream.DisposeAsync();

        return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
    }

    [HttpPost("posts/{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentRequest request, CancellationToken ct = default)
    {
        var comment = await _communityService.AddCommentAsync(id, CurrentUserId, request, ct);
        return Ok(comment);
    }

    [HttpPost("posts/{id:guid}/likes")]
    public async Task<IActionResult> ToggleLike(Guid id, CancellationToken ct = default)
    {
        var result = await _communityService.ToggleLikeAsync(id, CurrentUserId, ct);
        return Ok(result);
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
