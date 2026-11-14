using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.FullName).HasMaxLength(100);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);

        // Identity creates a non-unique index on NormalizedEmail; make it unique.
        // SQL Server filters out NULLs, so cleared emails on deleted accounts do not clash.
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}
