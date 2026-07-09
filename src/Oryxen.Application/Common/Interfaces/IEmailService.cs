namespace Oryxen.Application.Common.Interfaces;

/// <summary>
/// Outbound email delivery abstraction. Implementations wrap an external provider
/// such as SendGrid, SMTP, or a console logger for development.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
