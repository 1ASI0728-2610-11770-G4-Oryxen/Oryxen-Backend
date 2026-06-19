using Oryxen.Domain.Entities;

namespace Oryxen.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
