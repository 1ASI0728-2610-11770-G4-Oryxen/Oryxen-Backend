using Microsoft.EntityFrameworkCore;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Domain.Common;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work for the Oryxen backend. Maps the Sprint 1 aggregates and applies
/// audit timestamps automatically on save.
/// </summary>
public sealed class OryxenDbContext : DbContext, IUnitOfWork
{
    public OryxenDbContext(DbContextOptions<OryxenDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> Users => Set<UserAccount>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<TelemetryData> TelemetryReadings => Set<TelemetryData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OryxenDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
