using Microsoft.Extensions.Logging;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// Console-logging push notification sender for development environments. In production
/// the Firebase Admin SDK would be initialised with the service-account JSON from config.
/// </summary>
internal sealed class FirebaseFcmService : IPushNotificationService
{
    private readonly ILogger<FirebaseFcmService> _logger;

    public FirebaseFcmService(ILogger<FirebaseFcmService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[FCM] Would push to device {Token} | Title: {Title} | Body: {Body}",
            deviceToken[..Math.Min(deviceToken.Length, 12)], title, body);
        return Task.CompletedTask;
    }
}
