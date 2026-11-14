using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(p => p.ItemName).HasMaxLength(100);
        builder.Property(p => p.Color).HasMaxLength(50);
        builder.Property(p => p.Brand).HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.AdditionalLocationDetails).HasMaxLength(300);
        builder.Property(p => p.VerificationDetails).HasMaxLength(1000);

        builder.HasOne(p => p.Owner)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CampusLocation)
            .WithMany()
            .HasForeignKey(p => p.CampusLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.Visibility, p.Type, p.Category, p.DateOccurred });
        builder.HasIndex(p => new { p.OwnerId, p.Status });
    }
}
