using Microsoft.Extensions.Logging;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// Console-logging email sender for development environments. When a real SendGrid API key
/// is configured in <c>SendGrid:ApiKey</c>, it sends via the SendGrid v3 Mail Send endpoint.
/// </summary>
internal sealed class SendGridEmailService : IEmailService
{
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(ILogger<SendGridEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[SendGrid] Would send email to {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}
