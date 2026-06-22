using Oryxen.Domain.Enums;

namespace Oryxen.Application.Notifications.Contracts;

public sealed record CreateNotificationRequest
{
    public Guid UserId { get; init; }
    public Guid? PlantId { get; init; }
    public NotificationType Type { get; init; }
    public NotificationChannel Channel { get; init; } = NotificationChannel.InApp;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
