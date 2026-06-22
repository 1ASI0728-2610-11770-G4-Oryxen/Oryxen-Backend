using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Notifications.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(INotificationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetByUserAsync(userId, cancellationToken);
        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(ToResponse)
            .ToArray();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _repository.CountUnreadAsync(userId, cancellationToken);
    }

    public async Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            PlantId = request.PlantId,
            Type = request.Type,
            Channel = request.Channel,
            Title = request.Title,
            Message = request.Message,
            IsRead = false,
            SentAt = request.Channel != NotificationChannel.InApp ? DateTime.UtcNow : null
        };

        await _repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(notification);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null)
            return;

        notification.IsRead = true;
        _repository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static NotificationResponse ToResponse(Notification n) =>
        new(
            n.Id,
            n.UserId,
            n.PlantId,
            n.Type,
            n.Channel,
            n.Title,
            n.Message,
            n.IsRead,
            n.CreatedAt,
            n.SentAt);
}
