using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class UserModerationLogConfiguration : IEntityTypeConfiguration<UserModerationLog>
{
    public void Configure(EntityTypeBuilder<UserModerationLog> builder)
    {
        builder.Property(l => l.Reason).HasMaxLength(500);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, because SQL Server rejects two cascading foreign keys to the same table.
        builder.HasOne(l => l.Admin)
            .WithMany()
            .HasForeignKey(l => l.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
