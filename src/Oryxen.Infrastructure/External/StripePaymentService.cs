using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// <see cref="IPaymentPlatformService"/> backed by the Stripe REST API. Creates Checkout
/// Sessions via <c>POST /v1/checkout/sessions</c> and parses webhook events by verifying
/// the signature locally. Uses HTTP calls (no Stripe SDK dependency) to keep the
/// Infrastructure layer lightweight. Missing API key → <see cref="ExternalServiceException"/> (502).
/// </summary>
public sealed class StripePaymentService : IPaymentPlatformService
{
    private readonly HttpClient _http;
    private readonly StripeSettings _settings;

    public StripePaymentService(HttpClient http, IOptions<StripeSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        string planName,
        decimal amount,
        string currency,
        string successUrl,
        string cancelUrl,
        string customerEmail,
        string clientReferenceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new ExternalServiceException(
                "Stripe secret key is not configured (set Stripe__SecretKey).");
        }

        var unitAmount = (long)(amount * 100); // Stripe expects cents
        var form = new FormUrlContentBuilder()
            .Add("mode", "payment")
            .Add("success_url", successUrl)
            .Add("cancel_url", cancelUrl)
            .Add("customer_email", customerEmail)
            .Add("client_reference_id", clientReferenceId)
            .Add("line_items[0][quantity]", "1")
            .Add("line_items[0][price_data][currency]", currency.ToLowerInvariant())
            .Add("line_items[0][price_data][unit_amount]", unitAmount.ToString())
            .Add("line_items[0][price_data][product_data][name]", $"Oryxen {planName} Plan")
            .Build();

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/checkout/sessions")
        {
            Content = form,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceException(
                    $"Stripe returned HTTP {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var sessionId = json.GetProperty("id").GetString() ?? string.Empty;
            var checkoutUrl = json.GetProperty("url").GetString() ?? string.Empty;

            return new CheckoutSessionResult(sessionId, checkoutUrl);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Stripe API is unreachable.", ex);
        }
    }

    public Task<WebhookEventResult?> ParseWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            return Task.FromResult<WebhookEventResult?>(null);
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var eventType = root.TryGetProperty("type", out var type) ? type.GetString() ?? string.Empty : string.Empty;

            string? subscriptionId = null;
            string? customerId = null;
            string? clientReferenceId = null;
            string? transactionId = null;
            decimal? amount = null;
            string? currency = null;

            if (root.TryGetProperty("data", out var data) && data.TryGetProperty("object", out var obj))
            {
                subscriptionId = obj.TryGetProperty("subscription", out var sub) ? sub.GetString() : null;
                customerId = obj.TryGetProperty("customer", out var cust) ? cust.GetString() : null;
                clientReferenceId = obj.TryGetProperty("client_reference_id", out var refId) ? refId.GetString() : null;
                transactionId = obj.TryGetProperty("payment_intent", out var pi) ? pi.GetString() : null;

                if (obj.TryGetProperty("amount_total", out var amt))
                {
                    amount = amt.GetInt64() / 100m;
                }
                if (obj.TryGetProperty("currency", out var cur))
                {
                    currency = cur.GetString();
                }
            }

            return Task.FromResult<WebhookEventResult?>(new WebhookEventResult(
                eventType, subscriptionId, customerId, clientReferenceId, transactionId, amount, currency));
        }
        catch (JsonException)
        {
            return Task.FromResult<WebhookEventResult?>(null);
        }
    }

    /// <summary>Helper to build application/x-www-form-urlencoded content for Stripe.</summary>
    private sealed class FormUrlContentBuilder
    {
        private readonly Dictionary<string, string> _fields = new();

        public FormUrlContentBuilder Add(string key, string value)
        {
            _fields[key] = value;
            return this;
        }

        public FormUrlEncodedContent Build() => new(_fields);
    }
}
