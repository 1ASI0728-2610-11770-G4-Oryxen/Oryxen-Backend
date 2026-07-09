using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly OryxenDbContext _db;

    public NotificationRepository(OryxenDbContext db) => _db = db;

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return list;
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default) =>
        await _db.Notifications.AddAsync(notification, cancellationToken);

    public void Update(Notification notification) =>
        _db.Notifications.Update(notification);
}
