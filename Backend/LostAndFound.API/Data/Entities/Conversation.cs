namespace LostAndFound.Api.Data.Entities;

/// <summary>A chat about one post. The other participant is always the post's owner.</summary>
public sealed class Conversation
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Post Post { get; set; } = null!;

    /// <summary>The user who clicked "Message Owner".</summary>
    public Guid StarterId { get; set; }

    public User Starter { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastMessageAt { get; set; }

    public ICollection<Message> Messages { get; } = [];
}
