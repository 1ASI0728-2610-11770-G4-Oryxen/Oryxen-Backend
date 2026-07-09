using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// <see cref="IEmailService"/> backed by the SendGrid v3 Mail Send API. When
/// <c>SendGrid:ApiKey</c> is configured it performs a real HTTP send; without a key it
/// logs the message and returns (explicit no-op fallback for local development, so the
/// notification flow remains demonstrable offline). Send failures are logged and do not
/// break the caller: email is a best-effort side channel of the Notification context.
/// </summary>
internal sealed class SendGridEmailService : IEmailService
{
    private const string MailSendEndpoint = "https://api.sendgrid.com/v3/mail/send";

    private readonly HttpClient _http;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(HttpClient http, IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = configuration["SendGrid:ApiKey"] ?? string.Empty;
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@oryxen.io";
        _fromName = configuration["SendGrid:FromName"] ?? "Oryxen Alerts";
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogInformation(
                "[SendGrid] No API key configured — email NOT sent (dev fallback). To: {To} | Subject: {Subject}",
                to, subject);
            return;
        }

        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = to } } } },
            from = new { email = _fromEmail, name = _fromName },
            subject,
            content = new[] { new { type = "text/plain", value = body } }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, MailSendEndpoint)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[SendGrid] Email sent to {To} | Subject: {Subject}", to, subject);
            }
            else
            {
                _logger.LogWarning(
                    "[SendGrid] Mail Send returned HTTP {Status} for {To}; the notification remains recorded in-app.",
                    (int)response.StatusCode, to);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "[SendGrid] Email delivery to {To} failed; the notification remains recorded in-app.", to);
        }
    }
}
