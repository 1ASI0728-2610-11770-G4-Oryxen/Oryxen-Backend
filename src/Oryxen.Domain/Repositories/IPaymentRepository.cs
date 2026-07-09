using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

/// <summary>
/// Persistence contract for the <see cref="Payment"/> entity. Payments are appended
/// on every billing event (checkout, renewal, refund).
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
