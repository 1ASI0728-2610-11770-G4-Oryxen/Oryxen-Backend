using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.RefreshTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(u => u.RefreshTokenHash);

        builder.HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(join => join.ToTable("user_roles"));

        builder.HasOne(u => u.Subscription)
            .WithOne(s => s.UserAccount)
            .HasForeignKey<Subscription>(s => s.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
