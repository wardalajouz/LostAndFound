using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Conversations;

/// <summary>Shatha owns a found post; Ward messages her about it; Ahmad is a stranger.</summary>
public sealed class MessagingTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- Sending ----------

    [Fact]
    public async Task Both_participants_can_send_and_see_whose_message_is_whose()
    {
        Chat chat = await NewChatAsync();

        MessageResponse fromWard = await _client.SendMessageOkAsync(chat.Ward, chat.Id, "  Hi, I think it's my phone  ");
        MessageResponse fromShatha = await _client.SendMessageOkAsync(chat.Shatha, chat.Id, "What is the wallpaper?");

        Assert.Equal("Hi, I think it's my phone", fromWard.Content);
        Assert.True(fromWard.IsMine);
        Assert.Null(fromWard.ReadAt);

        var wardView = await _client.MessagesAsync(chat.Ward, chat.Id);
        var shathaView = await _client.MessagesAsync(chat.Shatha, chat.Id);
        Assert.Equal([fromShatha.Id, fromWard.Id], wardView.Items.Select(m => m.Id));
        Assert.Equal([false, true], wardView.Items.Select(m => m.IsMine));
        Assert.Equal([true, false], shathaView.Items.Select(m => m.IsMine));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public async Task Empty_messages_are_rejected(string content)
    {
        Chat chat = await NewChatAsync();

        HttpResponseMessage response = await _client.SendMessageAsync(chat.Ward, chat.Id, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("Content", out _));
        Assert.Equal(0, (await _client.MessagesAsync(chat.Ward, chat.Id)).TotalCount);
    }

    [Fact]
    public async Task Message_length_limit_is_2000_characters()
    {
        Chat chat = await NewChatAsync();

        HttpResponseMessage atLimit = await _client.SendMessageAsync(chat.Ward, chat.Id, new string('a', 2000));
        HttpResponseMessage overLimit = await _client.SendMessageAsync(chat.Ward, chat.Id, new string('a', 2001));

        Assert.Equal(HttpStatusCode.Created, atLimit.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, overLimit.StatusCode);
    }

    [Fact]
    public async Task Arabic_and_emoji_are_stored_exactly()
    {
        Chat chat = await NewChatAsync();
        const string text = "مرحبا، هل الجزدان أسود؟ 👛";

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, text);

        Assert.Equal(text, Assert.Single((await _client.MessagesAsync(chat.Shatha, chat.Id)).Items).Content);
    }

    [Fact]
    public async Task Stranger_cannot_send_read_or_open_the_chat()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "secret handover place");
        string ahmad = await NewUserAsync();

        HttpResponseMessage send = await _client.SendMessageAsync(ahmad, chat.Id, "let me in");
        HttpResponseMessage read = await _client.SendAsync(HttpMethod.Get, $"/api/conversations/{chat.Id}/messages", ahmad);
        HttpResponseMessage open = await _client.SendAsync(HttpMethod.Get, $"/api/conversations/{chat.Id}", ahmad);
        HttpResponseMessage markRead = await _client.MarkReadAsync(ahmad, chat.Id);

        Assert.Equal(HttpStatusCode.NotFound, send.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, open.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, markRead.StatusCode);
        Assert.DoesNotContain("secret handover place", await read.Content.ReadAsStringAsync());
        Assert.Equal(1, (await _client.MessagesAsync(chat.Ward, chat.Id)).TotalCount);
        Assert.Equal(1, (await _client.ChatAsync(chat.Shatha, chat.Id)).UnreadCount);
    }

    [Fact]
    public async Task Unknown_chat_returns_404()
    {
        string ward = await NewUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendMessageAsync(ward, Guid.NewGuid(), "hi")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(HttpMethod.Get, $"/api/conversations/{Guid.NewGuid()}/messages", ward)).StatusCode);
    }

    [Theory]
    [InlineData("resolve")]
    [InlineData("delete")]
    [InlineData("hide")]
    public async Task Chat_keeps_working_after_the_post_is_resolved_deleted_or_hidden(string change)
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "before");
        switch (change)
        {
            case "resolve":
                await _client.SendAsync(HttpMethod.Post, $"/api/posts/{chat.PostId}/resolve", chat.Shatha);
                break;
            case "delete":
                await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{chat.PostId}", chat.Shatha);
                break;
            default:
                await factory.WithDbAsync(db => db.Posts.Where(p => p.Id == chat.PostId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, PostVisibility.Hidden)));
                break;
        }

        HttpResponseMessage fromWard = await _client.SendMessageAsync(chat.Ward, chat.Id, "after");
        HttpResponseMessage fromShatha = await _client.SendMessageAsync(chat.Shatha, chat.Id, "after too");

        Assert.Equal(HttpStatusCode.Created, fromWard.StatusCode);
        Assert.Equal(HttpStatusCode.Created, fromShatha.StatusCode);
        Assert.Equal(3, (await _client.MessagesAsync(chat.Shatha, chat.Id)).TotalCount);
    }

    // ---------- Reading ----------

    [Fact]
    public async Task Messages_are_newest_first_and_paged()
    {
        Chat chat = await NewChatAsync();
        var sent = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            sent.Add((await _client.SendMessageOkAsync(i % 2 == 0 ? chat.Ward : chat.Shatha, chat.Id, $"message {i}")).Id);
        }

        var first = await _client.MessagesAsync(chat.Ward, chat.Id, "pageSize=2&page=1");
        var last = await _client.MessagesAsync(chat.Ward, chat.Id, "pageSize=2&page=3");

        Assert.Equal(5, first.TotalCount);
        Assert.Equal([sent[4], sent[3]], first.Items.Select(m => m.Id));
        Assert.Equal(sent[0], Assert.Single(last.Items).Id);
    }

    [Fact]
    public async Task Reading_messages_does_not_mark_them_as_read()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hello");

        await _client.MessagesAsync(chat.Shatha, chat.Id);

        Assert.Equal(1, (await _client.ChatAsync(chat.Shatha, chat.Id)).UnreadCount);
    }

    [Fact]
    public async Task Mark_as_read_clears_only_the_other_persons_messages()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "one");
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "two");
        await _client.SendMessageOkAsync(chat.Shatha, chat.Id, "reply");
        Assert.Equal(2, (await _client.ChatAsync(chat.Shatha, chat.Id)).UnreadCount);
        Assert.Equal(1, (await _client.ChatAsync(chat.Ward, chat.Id)).UnreadCount);

        HttpResponseMessage response = await _client.MarkReadAsync(chat.Shatha, chat.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, (await _client.ChatAsync(chat.Shatha, chat.Id)).UnreadCount);
        Assert.Equal(1, (await _client.ChatAsync(chat.Ward, chat.Id)).UnreadCount);

        var wardView = await _client.MessagesAsync(chat.Ward, chat.Id);
        Assert.All(wardView.Items.Where(m => m.IsMine), m => Assert.NotNull(m.ReadAt));
        Assert.All(wardView.Items.Where(m => !m.IsMine), m => Assert.Null(m.ReadAt));
    }

    [Fact]
    public async Task Mark_as_read_twice_keeps_the_first_read_time()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hello");
        await _client.MarkReadAsync(chat.Shatha, chat.Id);
        DateTimeOffset? firstRead = Assert.Single((await _client.MessagesAsync(chat.Ward, chat.Id)).Items).ReadAt;

        HttpResponseMessage again = await _client.MarkReadAsync(chat.Shatha, chat.Id);

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.NotNull(firstRead);
        Assert.Equal(firstRead, Assert.Single((await _client.MessagesAsync(chat.Ward, chat.Id)).Items).ReadAt);
    }

    [Fact]
    public async Task New_message_after_reading_is_unread_again()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "first");
        await _client.MarkReadAsync(chat.Shatha, chat.Id);

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "second");

        Assert.Equal(1, (await _client.ChatAsync(chat.Shatha, chat.Id)).UnreadCount);
    }

    [Fact]
    public async Task Chat_header_shows_the_last_message_and_the_other_person_from_each_side()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "first");
        MessageResponse last = await _client.SendMessageOkAsync(chat.Shatha, chat.Id, "latest");

        ConversationResponse wardHeader = await _client.ChatAsync(chat.Ward, chat.Id);
        ConversationResponse shathaHeader = await _client.ChatAsync(chat.Shatha, chat.Id);

        Assert.Equal(last.Id, wardHeader.LastMessage!.Id);
        Assert.False(wardHeader.LastMessage.IsMine);
        Assert.True(shathaHeader.LastMessage!.IsMine);
        Assert.Equal(chat.ShathaId, wardHeader.OtherParticipant.Id);
        Assert.Equal(chat.WardId, shathaHeader.OtherParticipant.Id);
    }

    [Fact]
    public async Task Sending_updates_the_last_message_time()
    {
        Chat chat = await NewChatAsync();

        MessageResponse message = await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hello");

        await factory.WithDbAsync(async db =>
            Assert.Equal(message.SentAt, (await db.Conversations.SingleAsync(c => c.Id == chat.Id)).LastMessageAt));
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

    private async Task<Chat> NewChatAsync()
    {
        string shatha = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        string ward = await NewUserAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);
        Guid wardId = (await _client.CreatePostAsync(ward, PostApi.Body())).Publisher.Id;
        return new Chat(chat.Id, post.Id, shatha, post.Publisher.Id, ward, wardId);
    }

    private sealed record Chat(Guid Id, Guid PostId, string Shatha, Guid ShathaId, string Ward, Guid WardId);
}
