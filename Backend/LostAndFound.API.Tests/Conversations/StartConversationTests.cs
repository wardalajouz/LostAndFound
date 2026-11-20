using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Conversations;

public sealed class StartConversationTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Message_owner_starts_a_chat_with_the_posts_owner()
    {
        (string owner, PostResponse post) = await NewPostAsync();
        var image = Assert.Single(await _client.UploadPostImagesOkAsync(post.Id, owner, ImageApi.Jpeg()));
        string ward = await NewUserAsync();

        HttpResponseMessage response = await _client.StartChatAsync(ward, post.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ConversationResponse chat = (await response.Content.ReadFromJsonAsync<ConversationResponse>(ApiClientExtensions.Json))!;
        Assert.Equal(post.Id, chat.Post.Id);
        Assert.Equal(post.ItemName, chat.Post.ItemName);
        Assert.Equal(PostType.Found, chat.Post.Type);
        Assert.Equal(image.Url, chat.Post.ThumbnailUrl);
        Assert.Equal(post.Publisher.Id, chat.OtherParticipant.Id);
        Assert.Null(chat.LastMessage);
        Assert.Equal(0, chat.UnreadCount);
    }

    [Fact]
    public async Task Starting_again_returns_the_same_chat()
    {
        (_, PostResponse post) = await NewPostAsync();
        string ward = await NewUserAsync();

        ConversationResponse first = await _client.StartChatOkAsync(ward, post.Id);
        ConversationResponse second = await _client.StartChatOkAsync(ward, post.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await CountChatsAsync(post.Id));
    }

    [Fact]
    public async Task Different_people_get_separate_chats_about_the_same_post()
    {
        (_, PostResponse post) = await NewPostAsync();
        string ward = await NewUserAsync();
        string ahmad = await NewUserAsync();

        ConversationResponse wardChat = await _client.StartChatOkAsync(ward, post.Id);
        ConversationResponse ahmadChat = await _client.StartChatOkAsync(ahmad, post.Id);

        Assert.NotEqual(wardChat.Id, ahmadChat.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(HttpMethod.Get, $"/api/conversations/{wardChat.Id}", ahmad)).StatusCode);
    }

    [Fact]
    public async Task Owner_cannot_message_themselves()
    {
        (string owner, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await _client.StartChatAsync(owner, post.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("PostId", out _));
        Assert.Equal(0, await CountChatsAsync(post.Id));
    }

    [Fact]
    public async Task Unknown_post_returns_404()
    {
        string ward = await NewUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.StartChatAsync(ward, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.StartChatAsync(ward, Guid.Empty)).StatusCode);
    }

    [Theory]
    [InlineData(PostVisibility.Hidden)]
    [InlineData(PostVisibility.Deleted)]
    public async Task Hidden_or_deleted_post_returns_404(PostVisibility visibility)
    {
        (_, PostResponse post) = await NewPostAsync();
        await SetVisibilityAsync(post.Id, visibility);

        HttpResponseMessage response = await _client.StartChatAsync(await NewUserAsync(), post.Id);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Resolved_post_refuses_new_chats_but_reopens_existing_ones()
    {
        (string owner, PostResponse post) = await NewPostAsync();
        string ward = await NewUserAsync();
        ConversationResponse existing = await _client.StartChatOkAsync(ward, post.Id);
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{post.Id}/resolve", owner);

        HttpResponseMessage newcomer = await _client.StartChatAsync(await NewUserAsync(), post.Id);
        ConversationResponse reopened = await _client.StartChatOkAsync(ward, post.Id);

        Assert.Equal(HttpStatusCode.Conflict, newcomer.StatusCode);
        Assert.Equal(existing.Id, reopened.Id);
    }

    [Fact]
    public async Task Missing_post_id_returns_400()
    {
        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, "/api/conversations", await NewUserAsync(), new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Two_simultaneous_starts_end_with_one_chat()
    {
        (_, PostResponse post) = await NewPostAsync();
        string ward = await NewUserAsync();
        Guid wardId = (await _client.GetAsync<PostResponse>($"/api/posts/{(await _client.CreatePostAsync(ward, PostApi.Body())).Id}")).Publisher.Id;
        Guid rivalId = Guid.NewGuid();

        // Just before this request saves, another request "wins" and inserts the same chat.
        factory.SaveHooks.BeforeNextSave(async context =>
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Conversations (Id, PostId, StarterId, CreatedAt) VALUES ({rivalId}, {post.Id}, {wardId}, {0L})");
        });

        HttpResponseMessage response = await _client.StartChatAsync(ward, post.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ConversationResponse chat = (await response.Content.ReadFromJsonAsync<ConversationResponse>(ApiClientExtensions.Json))!;
        Assert.Equal(rivalId, chat.Id);
        Assert.Equal(1, await CountChatsAsync(post.Id));
    }

    [Fact]
    public async Task Other_save_failures_are_not_hidden()
    {
        (_, PostResponse post) = await NewPostAsync();
        string ward = await NewUserAsync();
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.StartChatAsync(ward, post.Id);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(0, await CountChatsAsync(post.Id));
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

    private async Task<(string Token, PostResponse Post)> NewPostAsync()
    {
        string token = await NewUserAsync();
        return (token, await _client.CreatePostAsync(token, PostApi.Body()));
    }

    private async Task<int> CountChatsAsync(Guid postId)
    {
        int count = 0;
        await factory.WithDbAsync(async db => count = await db.Conversations.CountAsync(c => c.PostId == postId));
        return count;
    }

    private Task SetVisibilityAsync(Guid postId, PostVisibility visibility) =>
        factory.WithDbAsync(db => db.Posts.Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, visibility)));
}
