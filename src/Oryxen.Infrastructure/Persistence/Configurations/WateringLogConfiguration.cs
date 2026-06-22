using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

public sealed class WateringLogConfiguration : IEntityTypeConfiguration<WateringLog>
{
    public void Configure(EntityTypeBuilder<WateringLog> builder)
    {
        builder.ToTable("watering_logs");

        builder.HasKey(w => w.Id);

        builder.HasIndex(w => w.PlantId);
    }
}
