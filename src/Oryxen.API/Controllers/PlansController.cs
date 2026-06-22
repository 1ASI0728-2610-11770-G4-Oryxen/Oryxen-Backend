using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Billing;
using Oryxen.Application.Billing.Contracts;
using Oryxen.Domain.Constants;

namespace Oryxen.API.Controllers;

/// <summary>
/// Billing catalog: exposes the public plan listing. No authentication required so
/// prospective customers can browse pricing before signing up.
/// </summary>
[ApiController]
[Route("api/v1/plans")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class PlansController : ControllerBase
{
    private readonly IPlanService _plans;

    public PlansController(IPlanService plans) => _plans = plans;

    /// <summary>Returns all active plans sorted by price ascending.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlanResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanResponse>>> GetAll(CancellationToken ct) =>
        Ok(await _plans.GetAllAsync(ct));
}
