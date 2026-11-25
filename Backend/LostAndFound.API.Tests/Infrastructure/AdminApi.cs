using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Tests.Infrastructure;

public static class AdminApi
{
    public static Task<string> AdminTokenAsync(this HttpClient client) =>
        client.LoginAndGetTokenAsync(AdminApiFactory.AdminEmail, AdminApiFactory.AdminPassword);

    public static Task<AdminStatsResponse> AdminStatsAsync(this HttpClient client, string token, string? language = null) =>
        client.GetAsync<AdminStatsResponse>("/api/admin/stats", token, language);

    public static Task<PagedResponse<AdminPostResponse>> AdminPostsAsync(this HttpClient client, string token, string query = "") =>
        client.GetAsync<PagedResponse<AdminPostResponse>>($"/api/admin/posts?{query}", token);

    public static Task<AdminPostDetailsResponse> AdminPostAsync(this HttpClient client, string token, Guid postId) =>
        client.GetAsync<AdminPostDetailsResponse>($"/api/admin/posts/{postId}", token);

    public static Task<HttpResponseMessage> SetVisibilityAsync(this HttpClient client, string token, Guid postId, string visibility, string reason = "Breaks the rules") =>
        client.SendAsync(HttpMethod.Put, $"/api/admin/posts/{postId}/visibility", token, new { visibility, reason });

    public static Task<PagedResponse<AdminUserResponse>> AdminUsersAsync(this HttpClient client, string token, string query = "") =>
        client.GetAsync<PagedResponse<AdminUserResponse>>($"/api/admin/users?{query}", token);

    public static Task<HttpResponseMessage> SetAccountStatusAsync(this HttpClient client, string token, Guid userId, string status, string reason = "Spam reports") =>
        client.SendAsync(HttpMethod.Put, $"/api/admin/users/{userId}/status", token, new { status, reason });
}
