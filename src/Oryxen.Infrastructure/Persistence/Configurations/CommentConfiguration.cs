using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oryxen.Domain.Entities;

namespace Oryxen.Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PostId).IsRequired();
        builder.HasIndex(c => c.PostId);

        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId);

        builder.Property(c => c.Content).IsRequired().HasMaxLength(1000);

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
