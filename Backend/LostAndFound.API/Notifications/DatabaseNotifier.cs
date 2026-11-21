using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Notifications;

/// <summary>Stores in-app notifications, respecting each recipient's notification settings.</summary>
public sealed class DatabaseNotifier(AppDbContext db) : INotifier
{
    public async Task NotifyNewMessageAsync(
        Guid recipientId, Guid conversationId, Guid postId, DateTimeOffset sentAt, CancellationToken cancellationToken)
    {
        bool wantsIt = await db.Users
            .Where(u => u.Id == recipientId && u.DeletedAt == null)
            .Select(u => u.NotifyOnNewMessage)
            .FirstOrDefaultAsync(cancellationToken);

        if (!wantsIt)
        {
            return;
        }

        Notification? unread = await db.Notifications.FirstOrDefaultAsync(
            n => n.UserId == recipientId && n.ConversationId == conversationId
                && n.Type == NotificationType.NewMessage && !n.IsRead,
            cancellationToken);

        if (unread is not null)
        {
            unread.CreatedAt = sentAt;
            return;
        }

        db.Notifications.Add(new Notification
        {
            UserId = recipientId,
            Type = NotificationType.NewMessage,
            ConversationId = conversationId,
            PostId = postId,
            CreatedAt = sentAt
        });
    }

    public async Task NotifyPostResolvedAsync(Guid postId, DateTimeOffset resolvedAt, CancellationToken cancellationToken)
    {
        // Starters are never the owner (owners cannot message themselves), so the owner is not notified.
        List<Guid> recipients = await db.Conversations
            .Where(c => c.PostId == postId && c.Starter.DeletedAt == null && c.Starter.NotifyOnPostUpdate)
            .Select(c => c.StarterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        db.Notifications.AddRange(recipients.Select(userId => new Notification
        {
            UserId = userId,
            Type = NotificationType.PostResolved,
            PostId = postId,
            CreatedAt = resolvedAt
        }));
    }

    public async Task MarkChatNotificationsReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        await db.Notifications
            .Where(n => n.UserId == userId && n.ConversationId == conversationId
                && n.Type == NotificationType.NewMessage && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);
}
