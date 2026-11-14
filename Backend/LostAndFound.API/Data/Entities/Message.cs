namespace LostAndFound.Api.Data.Entities;

public sealed class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public Guid SenderId { get; set; }

    public User Sender { get; set; } = null!;

    public required string Content { get; set; }

    public DateTimeOffset SentAt { get; set; }

    /// <summary>Null until the other participant reads it.</summary>
    public DateTimeOffset? ReadAt { get; set; }
}
