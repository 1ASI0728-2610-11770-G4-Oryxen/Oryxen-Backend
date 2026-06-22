namespace Oryxen.Domain.Entities;

public sealed class Like
{
    public Guid PostId { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CommunityPost Post { get; set; } = null!;

    public UserAccount User { get; set; } = null!;
}
