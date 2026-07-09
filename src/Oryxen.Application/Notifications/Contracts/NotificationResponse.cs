using Oryxen.Domain.Enums;

namespace Oryxen.Application.Notifications.Contracts;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    Guid? PlantId,
    NotificationType Type,
    NotificationChannel Channel,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? SentAt);
