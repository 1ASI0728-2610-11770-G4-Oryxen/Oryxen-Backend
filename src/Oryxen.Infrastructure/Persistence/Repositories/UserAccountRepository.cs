using Microsoft.EntityFrameworkCore;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Repositories;

namespace Oryxen.Infrastructure.Persistence.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly OryxenDbContext _db;

    public UserAccountRepository(OryxenDbContext db) => _db = db;

    public Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.Roles)
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<UserAccount?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(UserAccount user, CancellationToken cancellationToken = default) =>
        await _db.Users.AddAsync(user, cancellationToken);

    public void Update(UserAccount user) => _db.Users.Update(user);
}
