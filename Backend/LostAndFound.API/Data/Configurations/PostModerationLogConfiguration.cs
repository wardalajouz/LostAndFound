using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class PostModerationLogConfiguration : IEntityTypeConfiguration<PostModerationLog>
{
    public void Configure(EntityTypeBuilder<PostModerationLog> builder)
    {
        builder.Property(l => l.Reason).HasMaxLength(500);

        builder.HasOne(l => l.Post)
            .WithMany()
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Admin)
            .WithMany()
            .HasForeignKey(l => l.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
