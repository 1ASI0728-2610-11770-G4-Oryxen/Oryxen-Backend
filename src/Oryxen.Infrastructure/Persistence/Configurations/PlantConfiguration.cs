using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

public sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("plants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Type).HasMaxLength(80).IsRequired();
        builder.Property(p => p.ImgUrl).HasMaxLength(512);
        builder.Property(p => p.Bio).HasMaxLength(2000);
        builder.Property(p => p.Location).HasMaxLength(200);
        builder.Property(p => p.AssignedDeviceId).HasMaxLength(64);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.UserAccountId);

        // Plants belong to a UserAccount (no inverse navigation on the principal).
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(p => p.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.WateringLogs)
            .WithOne()
            .HasForeignKey(w => w.PlantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
