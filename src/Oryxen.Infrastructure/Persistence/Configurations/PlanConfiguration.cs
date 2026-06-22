using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(60).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.Price).HasPrecision(10, 2).IsRequired();

        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.Property(p => p.Features).HasColumnType("text").IsRequired();

        builder.Property(p => p.StripePriceId).HasMaxLength(120);
    }
}
