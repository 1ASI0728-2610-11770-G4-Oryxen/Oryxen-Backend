using Microsoft.Extensions.Logging;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// FCM adapter — prepared but intentionally NOT a real push sender yet. Oryxen's domain
/// has no per-device FCM registration-token storage (clients never register device tokens),
/// so a real send is not possible regardless of credentials. This implementation logs the
/// would-be push so the Notification flow stays observable; in-app notifications (persisted
/// and served via /api/v1/notifications) are the delivered channel. Upgrading to real push
/// requires: (1) a DeviceToken entity + registration endpoint, (2) Firebase Admin SDK
/// initialised from FirebaseFcm:ServiceAccountJson, (3) replacing this log with a send.
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
