using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

internal sealed class CommunityRepository : ICommunityRepository
{
    private readonly OryxenDbContext _db;

    public CommunityRepository(OryxenDbContext db) => _db = db;

    public async Task<IReadOnlyList<CommunityPost>> GetFeedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.CommunityPosts
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public Task<CommunityPost?> GetPostByIdAsync(Guid postId, CancellationToken ct = default) =>
        _db.CommunityPosts
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Likes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == postId, ct);

    public async Task AddPostAsync(CommunityPost post, CancellationToken ct = default) =>
        await _db.CommunityPosts.AddAsync(post, ct);

    public async Task AddCommentAsync(Comment comment, CancellationToken ct = default) =>
        await _db.Comments.AddAsync(comment, ct);

    public Task<Comment?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default) =>
        _db.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);

    public void RemoveComment(Comment comment) =>
        _db.Comments.Remove(comment);

    public Task<Like?> GetLikeAsync(Guid postId, Guid userId, CancellationToken ct = default) =>
        _db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);

    public async Task AddLikeAsync(Like like, CancellationToken ct = default) =>
        await _db.Likes.AddAsync(like, ct);

    public void RemoveLike(Like like) =>
        _db.Likes.Remove(like);

    public Task<int> CountByPostAsync(Guid postId, CancellationToken ct = default) =>
        _db.Likes.CountAsync(l => l.PostId == postId, ct);
}
