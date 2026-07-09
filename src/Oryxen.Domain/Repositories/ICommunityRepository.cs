using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

public interface ICommunityRepository
{
    Task<IReadOnlyList<CommunityPost>> GetFeedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<CommunityPost?> GetPostByIdAsync(Guid postId, CancellationToken ct = default);

    Task AddPostAsync(CommunityPost post, CancellationToken ct = default);

    Task AddCommentAsync(Comment comment, CancellationToken ct = default);

    Task<Comment?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default);

    void RemoveComment(Comment comment);

    Task<Like?> GetLikeAsync(Guid postId, Guid userId, CancellationToken ct = default);

    Task AddLikeAsync(Like like, CancellationToken ct = default);

    void RemoveLike(Like like);

    Task<int> CountByPostAsync(Guid postId, CancellationToken ct = default);
}
