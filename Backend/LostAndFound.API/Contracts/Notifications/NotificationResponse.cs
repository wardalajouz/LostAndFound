namespace LostAndFound.Api.Contracts.Notifications;

/// <summary>PostId or ConversationId tells the frontend what to open when the notification is clicked.</summary>
public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    Guid? PostId,
    Guid? ConversationId);
