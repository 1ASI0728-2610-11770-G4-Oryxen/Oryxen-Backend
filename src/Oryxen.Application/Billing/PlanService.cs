using Oryxen.Application.Billing.Contracts;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.Billing;

/// <summary>
/// Read-only service for the public plan catalog. Returns all active plans sorted by price.
/// </summary>
public sealed class PlanService : IPlanService
{
    private readonly IPlanRepository _plans;

    public PlanService(IPlanRepository plans) => _plans = plans;

    public async Task<IReadOnlyList<PlanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _plans.GetAllAsync(cancellationToken);
        return plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => new PlanResponse(
                p.Id, p.Name, p.Price, p.Currency, p.BillingCycleMonths, p.Features, p.IsActive))
            .ToArray();
    }
}
