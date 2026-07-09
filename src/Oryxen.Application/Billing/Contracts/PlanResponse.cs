namespace Oryxen.Application.Billing.Contracts;

/// <summary>Projection of a plan for the public pricing catalog.</summary>
public sealed record PlanResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    int BillingCycleMonths,
    string Features,
    bool IsActive);
