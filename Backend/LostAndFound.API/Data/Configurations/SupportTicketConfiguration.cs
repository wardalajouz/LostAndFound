using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.Property(t => t.FullName).HasMaxLength(100);
        builder.Property(t => t.Email).HasMaxLength(256);
        builder.Property(t => t.Subject).HasMaxLength(150);
        builder.Property(t => t.Message).HasMaxLength(4000);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
