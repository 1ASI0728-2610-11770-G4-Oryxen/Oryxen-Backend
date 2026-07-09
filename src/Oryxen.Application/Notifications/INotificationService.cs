using Oryxen.Application.Notifications.Contracts;

namespace Oryxen.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationResponse>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
