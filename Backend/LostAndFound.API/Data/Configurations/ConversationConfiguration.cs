using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Api.Data.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasOne(c => c.Post)
            .WithMany()
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Starter)
            .WithMany()
            .HasForeignKey(c => c.StarterId)
            .OnDelete(DeleteBehavior.Restrict);

        // One conversation per post per person.
        builder.HasIndex(c => new { c.PostId, c.StarterId }).IsUnique();
    }
}
