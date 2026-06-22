using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

public sealed class AnalysisReportConfiguration : IEntityTypeConfiguration<AnalysisReport>
{
    public void Configure(EntityTypeBuilder<AnalysisReport> builder)
    {
        builder.ToTable("analysis_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.Format)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(r => r.FileContent)
            .HasColumnType("text");

        builder.HasIndex(r => r.UserAccountId);
        builder.HasIndex(r => new { r.UserAccountId, r.PlantId });
    }
}
