namespace Oryxen.Application.Community.Contracts;

public sealed record CreatePostRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

public sealed record CreateCommentRequest
{
    public string Content { get; init; } = string.Empty;
}

public sealed record PostResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int LikesCount { get; init; }
    public bool LikedByCurrentUser { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<CommentResponse> Comments { get; init; } = Array.Empty<CommentResponse>();
}

public sealed record CommentResponse
{
    public Guid Id { get; init; }
    public Guid PostId { get; init; }
    public Guid UserId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record LikeResponse
{
    public Guid PostId { get; init; }
    public int LikesCount { get; init; }
    public bool LikedByCurrentUser { get; init; }
}
