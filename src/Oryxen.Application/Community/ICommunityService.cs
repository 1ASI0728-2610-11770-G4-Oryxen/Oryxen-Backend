using Oryxen.Application.Community.Contracts;

namespace Oryxen.Application.Community;

public interface ICommunityService
{
    Task<IReadOnlyList<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<PostResponse?> GetPostByIdAsync(Guid postId, Guid currentUserId, CancellationToken ct = default);

    Task<PostResponse> CreatePostAsync(Guid userId, CreatePostRequest request, Stream? imageStream, string? imageFileName, CancellationToken ct = default);

    Task<CommentResponse> AddCommentAsync(Guid postId, Guid userId, CreateCommentRequest request, CancellationToken ct = default);

    Task<LikeResponse> ToggleLikeAsync(Guid postId, Guid userId, CancellationToken ct = default);
}
