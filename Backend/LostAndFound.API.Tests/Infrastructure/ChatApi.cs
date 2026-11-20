using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Conversations;

namespace LostAndFound.Api.Tests.Infrastructure;

public static class ChatApi
{
    public static Task<HttpResponseMessage> StartChatAsync(this HttpClient client, string token, Guid postId) =>
        client.SendAsync(HttpMethod.Post, "/api/conversations", token, new { postId });

    public static async Task<ConversationResponse> StartChatOkAsync(this HttpClient client, string token, Guid postId)
    {
        HttpResponseMessage response = await client.StartChatAsync(token, postId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ConversationResponse>(ApiClientExtensions.Json))!;
    }

    public static Task<HttpResponseMessage> SendMessageAsync(this HttpClient client, string token, Guid conversationId, string content) =>
        client.SendAsync(HttpMethod.Post, $"/api/conversations/{conversationId}/messages", token, new { content });

    public static async Task<MessageResponse> SendMessageOkAsync(this HttpClient client, string token, Guid conversationId, string content)
    {
        HttpResponseMessage response = await client.SendMessageAsync(token, conversationId, content);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MessageResponse>(ApiClientExtensions.Json))!;
    }

    public static Task<PagedResponse<ConversationResponse>> InboxAsync(this HttpClient client, string token, string query = "") =>
        client.GetAsync<PagedResponse<ConversationResponse>>($"/api/conversations?{query}", token);

    public static Task<ConversationResponse> ChatAsync(this HttpClient client, string token, Guid conversationId) =>
        client.GetAsync<ConversationResponse>($"/api/conversations/{conversationId}", token);

    public static Task<PagedResponse<MessageResponse>> MessagesAsync(this HttpClient client, string token, Guid conversationId, string query = "") =>
        client.GetAsync<PagedResponse<MessageResponse>>($"/api/conversations/{conversationId}/messages?{query}", token);

    public static Task<HttpResponseMessage> MarkReadAsync(this HttpClient client, string token, Guid conversationId) =>
        client.SendAsync(HttpMethod.Post, $"/api/conversations/{conversationId}/read", token);
}
