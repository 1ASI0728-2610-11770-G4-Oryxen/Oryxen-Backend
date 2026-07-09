using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SubscriptionId).IsRequired();
        builder.HasIndex(p => p.SubscriptionId);

        builder.Property(p => p.Amount).HasPrecision(10, 2).IsRequired();

        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(p => p.Provider).HasMaxLength(40).IsRequired();

        builder.Property(p => p.TransactionId).HasMaxLength(200);
    }
}
