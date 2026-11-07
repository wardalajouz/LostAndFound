using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Contracts.Notifications;

public sealed record NotificationsQuery : PagedQuery
{
    public bool UnreadOnly { get; init; }
}
