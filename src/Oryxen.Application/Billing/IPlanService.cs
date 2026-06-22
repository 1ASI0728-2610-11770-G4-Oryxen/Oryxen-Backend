using Oryxen.Application.Billing.Contracts;

namespace Oryxen.Application.Billing;

/// <summary>Application service for the Billing catalog (public plan listing).</summary>
public interface IPlanService
{
    Task<IReadOnlyList<PlanResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
