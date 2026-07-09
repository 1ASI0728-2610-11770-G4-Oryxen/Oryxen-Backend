using Oryxen.Infrastructure.External;
using Xunit;

namespace Oryxen.Application.Tests.Billing;

/// <summary>
/// Verifies the manual Stripe-Signature validation (t=timestamp,v1=HMAC-SHA256 over
/// "{t}.{payload}") that guards POST /api/v1/subscriptions/webhook against forged events.
/// </summary>
public class StripeWebhookSignatureTests
{
    private const string Secret = "whsec_test_secret_for_unit_tests_only";
    private const string Payload = """{"type":"checkout.session.completed","data":{"object":{}}}""";

    private static readonly DateTimeOffset Now = new(2026, 07, 09, 12, 00, 00, TimeSpan.Zero);

    private static string SignedHeader(DateTimeOffset at, string payload = Payload, string secret = Secret)
    {
        var t = at.ToUnixTimeSeconds();
        var v1 = StripeWebhookSignatureValidator.ComputeHmacSha256Hex($"{t}.{payload}", secret);
        return $"t={t},v1={v1}";
    }

    [Fact]
    public void Valid_Signature_Is_Accepted()
    {
        var header = SignedHeader(Now);

        Assert.True(StripeWebhookSignatureValidator.IsValid(Payload, header, Secret, utcNow: Now));
    }

    [Fact]
    public void Missing_Signature_Is_Rejected()
    {
        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, null, Secret, utcNow: Now));
        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, "", Secret, utcNow: Now));
    }

    [Fact]
    public void Forged_Signature_Is_Rejected()
    {
        var t = Now.ToUnixTimeSeconds();
        var header = $"t={t},v1={new string('a', 64)}";

        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, header, Secret, utcNow: Now));
    }

    [Fact]
    public void Signature_For_Different_Payload_Is_Rejected()
    {
        var header = SignedHeader(Now, payload: """{"type":"other"}""");

        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, header, Secret, utcNow: Now));
    }

    [Fact]
    public void Signature_With_Wrong_Secret_Is_Rejected()
    {
        var header = SignedHeader(Now, secret: "whsec_another_secret");

        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, header, Secret, utcNow: Now));
    }

    [Fact]
    public void Expired_Timestamp_Is_Rejected()
    {
        var header = SignedHeader(Now.AddMinutes(-10)); // beyond the 5-minute tolerance

        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, header, Secret, utcNow: Now));
    }

    [Fact]
    public void Header_Without_Timestamp_Is_Rejected()
    {
        var v1 = StripeWebhookSignatureValidator.ComputeHmacSha256Hex($"0.{Payload}", Secret);

        Assert.False(StripeWebhookSignatureValidator.IsValid(Payload, $"v1={v1}", Secret, utcNow: Now));
    }

    [Fact]
    public void Multiple_V1_Entries_Accept_When_One_Matches()
    {
        var t = Now.ToUnixTimeSeconds();
        var good = StripeWebhookSignatureValidator.ComputeHmacSha256Hex($"{t}.{Payload}", Secret);
        var header = $"t={t},v1={new string('b', 64)},v1={good}";

        Assert.True(StripeWebhookSignatureValidator.IsValid(Payload, header, Secret, utcNow: Now));
    }
}
