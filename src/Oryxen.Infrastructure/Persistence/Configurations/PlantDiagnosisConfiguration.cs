using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the <see cref="PlantDiagnosis"/> aggregate. The ImageUrl and
/// Recommendation columns are stored as <c>text</c> (PostgreSQL) because they may hold
/// a base64 data-URI and a long mitigation recommendation respectively.
/// </summary>
internal sealed class PlantDiagnosisConfiguration : IEntityTypeConfiguration<PlantDiagnosis>
{
    public void Configure(EntityTypeBuilder<PlantDiagnosis> builder)
    {
        builder.ToTable("plant_diagnoses");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.PlantId).IsRequired();
        builder.HasIndex(d => d.PlantId);

        builder.Property(d => d.ImageUrl).IsRequired().HasColumnType("text");

        builder.Property(d => d.DetectedPest).IsRequired().HasMaxLength(120);

        builder.Property(d => d.Recommendation).IsRequired().HasColumnType("text");

        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(d => d.CreatedAt).IsRequired();
    }
}
