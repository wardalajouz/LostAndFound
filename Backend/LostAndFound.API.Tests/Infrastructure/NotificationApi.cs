using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Notifications;

namespace LostAndFound.Api.Tests.Infrastructure;

public static class NotificationApi
{
    public static Task<PagedResponse<NotificationResponse>> NotificationsAsync(
        this HttpClient client, string token, string query = "", string? language = null) =>
        client.GetAsync<PagedResponse<NotificationResponse>>($"/api/notifications?{query}", token, language);

    public static async Task<int> UnreadCountAsync(this HttpClient client, string token) =>
        (await client.GetAsync<UnreadCountResponse>("/api/notifications/unread-count", token)).Count;

    public static Task<HttpResponseMessage> MarkNotificationReadAsync(this HttpClient client, string token, Guid id) =>
        client.SendAsync(HttpMethod.Post, $"/api/notifications/{id}/read", token);

    public static Task<HttpResponseMessage> MarkAllNotificationsReadAsync(this HttpClient client, string token) =>
        client.SendAsync(HttpMethod.Post, "/api/notifications/read-all", token);
}
