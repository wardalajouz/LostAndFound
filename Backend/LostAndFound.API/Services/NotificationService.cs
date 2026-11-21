using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>The signed-in user's notification center. Each user only ever sees and changes their own notifications.</summary>
public sealed class NotificationService(AppDbContext db, ICurrentUser currentUser, IRequestLanguage requestLanguage)
    : INotificationService
{
    public async Task<PagedResponse<NotificationResponse>> GetAsync(NotificationsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Notification> notifications = Own();
        if (query.UnreadOnly)
        {
            notifications = notifications.Where(n => !n.IsRead);
        }

        int totalCount = await notifications.CountAsync(cancellationToken);

        // Raw fields come from the database; the text is built in C#, in the request's language.
        var rows = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.IsRead,
                n.CreatedAt,
                n.PostId,
                n.ConversationId,
                ItemName = n.Post != null ? n.Post.ItemName : null,
                ActorName = n.Conversation == null
                    ? null
                    : n.Conversation.StarterId == n.UserId ? n.Conversation.Post.Owner.FullName : n.Conversation.Starter.FullName
            })
            .ToListAsync(cancellationToken);

        var language = requestLanguage.Current;
        List<NotificationResponse> items = rows
            .Select(row =>
            {
                (string title, string message) = NotificationText.For(row.Type, language, row.ItemName, row.ActorName);
                return new NotificationResponse(row.Id, row.Type, title, message, row.IsRead, row.CreatedAt, row.PostId, row.ConversationId);
            })
            .ToList();

        return new PagedResponse<NotificationResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken cancellationToken) =>
        new(await Own().CountAsync(n => !n.IsRead, cancellationToken));

    /// <summary>Someone else's notification gets the same 404 as a missing one. Marking twice is harmless.</summary>
    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        bool exists = await Own().AnyAsync(n => n.Id == id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Notification not found.");
        }

        await Own()
            .Where(n => n.Id == id && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken) =>
        await Own()
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), cancellationToken);

    private IQueryable<Notification> Own()
    {
        Guid userId = currentUser.Id;
        return db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
    }
}
