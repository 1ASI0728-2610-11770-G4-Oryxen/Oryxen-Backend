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

    public DbSet<Plant> Plants => Set<Plant>();

    public DbSet<WateringLog> WateringLogs => Set<WateringLog>();

    public DbSet<PlantDiagnosis> Diagnoses => Set<PlantDiagnosis>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Like> Likes => Set<Like>();

    public DbSet<AnalysisReport> AnalysisReports => Set<AnalysisReport>();

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
