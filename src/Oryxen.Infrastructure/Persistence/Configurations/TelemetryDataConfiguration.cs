using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

public sealed class TelemetryDataConfiguration : IEntityTypeConfiguration<TelemetryData>
{
    public void Configure(EntityTypeBuilder<TelemetryData> builder)
    {
        builder.ToTable("telemetry_data");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.DeviceId)
            .HasMaxLength(64)
            .IsRequired();

        // Primary access pattern: latest readings for a plant within a time window.
        builder.HasIndex(t => new { t.PlantId, t.RecordedAt });

        builder.HasIndex(t => t.DeviceId);
    }
}
