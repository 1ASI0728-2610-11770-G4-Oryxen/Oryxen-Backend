using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

public sealed class CommunityPost : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public int LikesCount { get; set; }

    public UserAccount Author { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
