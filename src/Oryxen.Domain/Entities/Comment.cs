using Oryxen.Domain.Common;

namespace Oryxen.Domain.Entities;

public sealed class Comment : AuditableEntity
{
    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public CommunityPost Post { get; set; } = null!;

    public UserAccount Author { get; set; } = null!;
}
