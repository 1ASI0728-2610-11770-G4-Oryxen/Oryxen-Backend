using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(UserAccount user, CancellationToken cancellationToken = default);

    void Update(UserAccount user);
}
