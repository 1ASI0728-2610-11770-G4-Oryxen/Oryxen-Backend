using System.Security.Cryptography;
using System.Text;

namespace Oryxen.Infrastructure.External;

/// <summary>
/// Verifies Stripe webhook signatures without depending on the Stripe SDK. Stripe signs
/// each webhook delivery with an HMAC-SHA256 over <c>"{timestamp}.{payload}"</c> using the
/// endpoint's webhook secret, and sends the result in the <c>Stripe-Signature</c> header
/// as <c>t=&lt;unix&gt;,v1=&lt;hex&gt;[,v1=...]</c>. See https://docs.stripe.com/webhooks#verify-manually.
/// </summary>
public static class StripeWebhookSignatureValidator
{
    /// <summary>Default replay-attack tolerance recommended by Stripe.</summary>
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="signatureHeader"/> contains a valid
    /// <c>v1</c> signature for <paramref name="payload"/> and its timestamp is within
    /// <paramref name="tolerance"/> of <paramref name="utcNow"/> (pass <c>null</c> for
    /// the current time; tests inject a fixed clock).
    /// </summary>
    public static bool IsValid(
        string payload,
        string? signatureHeader,
        string webhookSecret,
        TimeSpan? tolerance = null,
        DateTimeOffset? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            return false;
        }

        long timestamp = 0;
        var candidateSignatures = new List<string>();

        foreach (var part in signatureHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0) continue;

            var key = part[..separator];
            var value = part[(separator + 1)..];

            if (key == "t" && long.TryParse(value, out var parsed))
            {
                timestamp = parsed;
            }
            else if (key == "v1")
            {
                candidateSignatures.Add(value);
            }
        }

        if (timestamp == 0 || candidateSignatures.Count == 0)
        {
            return false;
        }

        var now = utcNow ?? DateTimeOffset.UtcNow;
        var age = now - DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var allowed = tolerance ?? DefaultTolerance;
        if (age > allowed || age < -allowed)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var expected = ComputeHmacSha256Hex(signedPayload, webhookSecret);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        foreach (var candidate in candidateSignatures)
        {
            var candidateBytes = Encoding.UTF8.GetBytes(candidate.ToLowerInvariant());
            if (candidateBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Computes the lowercase hex HMAC-SHA256 exactly as Stripe does (test helper too).</summary>
    public static string ComputeHmacSha256Hex(string signedPayload, string secret)
    {
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(signedPayload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
