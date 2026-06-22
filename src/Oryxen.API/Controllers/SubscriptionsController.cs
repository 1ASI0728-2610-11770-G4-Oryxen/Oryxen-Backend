using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Billing;
using Oryxen.Application.Billing.Contracts;
using Oryxen.Domain.Constants;

namespace Oryxen.API.Controllers;

/// <summary>
/// Subscription management: creates Stripe Checkout Sessions for plan upgrades and
/// receives Stripe webhook events for automated activation/renewal. The webhook endpoint
/// is anonymous (Stripe calls it server-to-server).
/// </summary>
[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptions;

    public SubscriptionsController(ISubscriptionService subscriptions) => _subscriptions = subscriptions;

    /// <summary>Creates a Stripe Checkout Session for upgrading to a Premium plan.</summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    public async Task<ActionResult<CheckoutResponse>> CreateCheckout(
        [FromBody] CheckoutRequest request,
        CancellationToken ct) =>
        Ok(await _subscriptions.CreateCheckoutAsync(CurrentUserId, request, ct));

    /// <summary>Returns the authenticated user's current subscription.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrent(CancellationToken ct) =>
        Ok(await _subscriptions.GetCurrentAsync(CurrentUserId, ct));

    /// <summary>
    /// Stripe webhook endpoint: receives raw payload + signature header. Anonymous
    /// because Stripe calls it server-to-server.
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await _subscriptions.ProcessWebhookAsync(payload, signature, ct);
        return Ok();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
