using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="Notification"/> aggregate. Notifications are
/// returned newest-first to match the convention used across the codebase.
/// </summary>
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    void Update(Notification notification);
}
