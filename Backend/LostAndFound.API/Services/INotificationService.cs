using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Notifications;

namespace LostAndFound.Api.Services;

public interface INotificationService
{
    Task<PagedResponse<NotificationResponse>> GetAsync(NotificationsQuery query, CancellationToken cancellationToken);

    Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken cancellationToken);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken);

    Task MarkAllAsReadAsync(CancellationToken cancellationToken);
}
