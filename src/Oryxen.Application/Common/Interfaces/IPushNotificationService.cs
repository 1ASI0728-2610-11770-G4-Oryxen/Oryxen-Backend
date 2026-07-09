namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Push notification delivery abstraction. Implementations wrap an external provider
/// such as Firebase Cloud Messaging (FCM).
/// </summary>
public interface IPushNotificationService
{
    Task SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default);
}
