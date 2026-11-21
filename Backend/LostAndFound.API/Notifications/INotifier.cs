namespace LostAndFound.Api.Notifications;

/// <summary>
/// Creates notifications when something happens. New and changed notifications are added to the
/// current database context and saved by the caller's SaveChanges, so a notification is stored
/// if and only if the action that caused it is stored.
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Tells the recipient about a new message. While they have an unread notification for this chat,
    /// that one is moved to the top instead of adding another.
    /// </summary>
    Task NotifyNewMessageAsync(Guid recipientId, Guid conversationId, Guid postId, DateTimeOffset sentAt, CancellationToken cancellationToken);

    /// <summary>Tells everyone who started a chat about the post that it has been resolved.</summary>
    Task NotifyPostResolvedAsync(Guid postId, DateTimeOffset resolvedAt, CancellationToken cancellationToken);

    /// <summary>Marks the user's new-message notifications for a chat as read. Runs immediately.</summary>
    Task MarkChatNotificationsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
}
