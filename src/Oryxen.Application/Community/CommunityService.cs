using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Community.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.Community;

public sealed class CommunityService : ICommunityService
{
    private readonly ICommunityRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageMetadataSanitizer _imageSanitizer;

    public CommunityService(ICommunityRepository repository, IUnitOfWork unitOfWork, IImageMetadataSanitizer imageSanitizer)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _imageSanitizer = imageSanitizer;
    }

    public async Task<IReadOnlyList<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var posts = await _repository.GetFeedAsync(page, pageSize, ct);
        return posts.Select(p => MapToPostResponse(p, currentUserId)).ToArray();
    }

    public async Task<PostResponse?> GetPostByIdAsync(Guid postId, Guid currentUserId, CancellationToken ct = default)
    {
        var post = await _repository.GetPostByIdAsync(postId, ct);
        return post is null ? null : MapToPostResponse(post, currentUserId);
    }

    public async Task<PostResponse> CreatePostAsync(Guid userId, CreatePostRequest request, Stream? imageStream, string? imageFileName, CancellationToken ct = default)
    {
        string? imageUrl = null;

        if (imageStream is not null && !string.IsNullOrWhiteSpace(imageFileName))
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "community");
            Directory.CreateDirectory(uploadsDir);

            var sanitizedStream = _imageSanitizer.StripExifMetadata(imageStream);
            var fileName = $"{Guid.NewGuid()}_{imageFileName}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var fileStream = File.Create(filePath);
            await sanitizedStream.CopyToAsync(fileStream, ct);
            await fileStream.FlushAsync(ct);

            imageUrl = $"/uploads/community/{fileName}";
        }

        var post = new CommunityPost
        {
            UserId = userId,
            Title = request.Title,
            Content = request.Content,
            ImageUrl = imageUrl
        };

        await _repository.AddPostAsync(post, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToPostResponse(post, userId);
    }

    public async Task<CommentResponse> AddCommentAsync(Guid postId, Guid userId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = request.Content
        };

        await _repository.AddCommentAsync(comment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var saved = await _repository.GetCommentByIdAsync(comment.Id, ct);
        var authorName = saved?.Author?.FullName ?? "Unknown";

        return new CommentResponse
        {
            Id = comment.Id,
            PostId = postId,
            UserId = userId,
            AuthorName = authorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<LikeResponse> ToggleLikeAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _repository.GetLikeAsync(postId, userId, ct);
        bool liked;

        if (existing is not null)
        {
            _repository.RemoveLike(existing);
            liked = false;
        }
        else
        {
            var like = new Like { PostId = postId, UserId = userId };
            await _repository.AddLikeAsync(like, ct);
            liked = true;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        var likesCount = await _repository.CountByPostAsync(postId, ct);

        return new LikeResponse { PostId = postId, LikesCount = likesCount, LikedByCurrentUser = liked };
    }

    private static PostResponse MapToPostResponse(CommunityPost post, Guid currentUserId)
    {
        return new PostResponse
        {
            Id = post.Id,
            UserId = post.UserId,
            AuthorName = post.Author?.FullName ?? "Unknown",
            Title = post.Title,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            LikesCount = post.Likes.Count,
            LikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
            CreatedAt = post.CreatedAt,
            Comments = post.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentResponse
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    UserId = c.UserId,
                    AuthorName = c.Author?.FullName ?? "Unknown",
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToArray()
        };
    }
}
