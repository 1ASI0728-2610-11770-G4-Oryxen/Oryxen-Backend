using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly OryxenDbContext _db;

    public RoleRepository(OryxenDbContext db) => _db = db;

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
}
