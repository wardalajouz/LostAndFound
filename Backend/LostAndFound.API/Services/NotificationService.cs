using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Notifications;

namespace LostAndFound.Api.Services;

public sealed class NotificationService : INotificationService
{
    public Task<PagedResponse<NotificationResponse>> GetAsync(NotificationsQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
