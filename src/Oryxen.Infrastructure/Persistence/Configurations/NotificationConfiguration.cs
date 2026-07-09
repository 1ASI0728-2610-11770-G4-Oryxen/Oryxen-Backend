using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId).IsRequired();
        builder.HasIndex(n => n.UserId);

        builder.Property(n => n.PlantId);

        builder.Property(n => n.Type).HasConversion<int>().IsRequired();

        builder.Property(n => n.Channel).HasConversion<int>().IsRequired();

        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);

        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);

        builder.Property(n => n.IsRead).IsRequired();

        builder.Property(n => n.SentAt);

        builder.Property(n => n.CreatedAt).IsRequired();
    }
}
