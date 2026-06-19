using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Constants;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // Stable identifiers so role seeding stays deterministic across migrations.
    private static readonly Guid FarmerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SupportTechnicianId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasData(
            new Role { Id = FarmerId, Name = Roles.Farmer, Description = "End user who owns plants and Sensor Lite devices." },
            new Role { Id = AdminId, Name = Roles.Admin, Description = "Platform administrator with full management access." },
            new Role { Id = SupportTechnicianId, Name = Roles.SupportTechnician, Description = "Support agent handling device and account incidents." });
    }
}
