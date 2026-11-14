using LostAndFound.Api.Contracts.Notifications;

namespace LostAndFound.Api.Data.Entities;

/// <summary>Title and text are not stored; they are built from Type in the user's language.</summary>
public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public Guid? PostId { get; set; }

    public Post? Post { get; set; }

    public Guid? ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
