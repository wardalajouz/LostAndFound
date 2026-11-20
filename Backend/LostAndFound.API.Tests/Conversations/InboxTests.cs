using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Conversations;

/// <summary>Each test uses fresh users, so their inboxes contain only that test's chats.</summary>
public sealed class InboxTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Starter_sees_a_new_chat_but_the_owner_only_after_the_first_message()
    {
        string shatha = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        string ward = await NewUserAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);

        Assert.Equal(chat.Id, Assert.Single((await _client.InboxAsync(ward)).Items).Id);
        Assert.Empty((await _client.InboxAsync(shatha)).Items);

        await _client.SendMessageOkAsync(ward, chat.Id, "hi");

        ConversationResponse ownerEntry = Assert.Single((await _client.InboxAsync(shatha)).Items);
        Assert.Equal(chat.Id, ownerEntry.Id);
        Assert.Equal(1, ownerEntry.UnreadCount);
        Assert.Equal("hi", ownerEntry.LastMessage!.Content);
    }

    [Fact]
    public async Task Owner_can_still_open_a_chat_with_no_messages_directly()
    {
        string shatha = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        ConversationResponse chat = await _client.StartChatOkAsync(await NewUserAsync(), post.Id);

        ConversationResponse opened = await _client.ChatAsync(shatha, chat.Id);

        Assert.Equal(chat.Id, opened.Id);
    }

    [Fact]
    public async Task Inbox_is_sorted_by_latest_activity_and_paged()
    {
        string shatha = await NewUserAsync();
        var chats = new List<(Guid Id, string Token)>();
        for (int i = 0; i < 3; i++)
        {
            PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body(itemName: $"Item {i}"));
            string other = await NewUserAsync();
            ConversationResponse chat = await _client.StartChatOkAsync(other, post.Id);
            await _client.SendMessageOkAsync(other, chat.Id, $"about item {i}");
            chats.Add((chat.Id, other));
        }

        Assert.Equal([chats[2].Id, chats[1].Id, chats[0].Id], (await _client.InboxAsync(shatha)).Items.Select(c => c.Id));

        await _client.SendMessageOkAsync(chats[0].Token, chats[0].Id, "still there?");

        var page1 = await _client.InboxAsync(shatha, "pageSize=2&page=1");
        var page2 = await _client.InboxAsync(shatha, "pageSize=2&page=2");
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal([chats[0].Id, chats[2].Id], page1.Items.Select(c => c.Id));
        Assert.Equal(chats[1].Id, Assert.Single(page2.Items).Id);
    }

    [Fact]
    public async Task Inbox_shows_the_right_other_person_and_post_on_each_side()
    {
        string shatha = await NewUserAsync();
        PostResponse shathaPost = await _client.CreatePostAsync(shatha, PostApi.Body(itemName: "Shatha's found phone"));
        string ward = await NewUserAsync();
        PostResponse wardPost = await _client.CreatePostAsync(ward, PostApi.Body(type: "Lost", itemName: "Ward's lost wallet"));

        // Ward messages Shatha about her post, and Shatha messages Ward about his.
        ConversationResponse aboutPhone = await _client.StartChatOkAsync(ward, shathaPost.Id);
        await _client.SendMessageOkAsync(ward, aboutPhone.Id, "is it mine?");
        ConversationResponse aboutWallet = await _client.StartChatOkAsync(shatha, wardPost.Id);
        await _client.SendMessageOkAsync(shatha, aboutWallet.Id, "I saw your wallet");

        var wardInbox = (await _client.InboxAsync(ward)).Items;
        var shathaInbox = (await _client.InboxAsync(shatha)).Items;

        Assert.Equal(2, wardInbox.Count);
        Assert.All(wardInbox, c => Assert.Equal(shathaPost.Publisher.Id, c.OtherParticipant.Id));
        Assert.All(shathaInbox, c => Assert.Equal(wardPost.Publisher.Id, c.OtherParticipant.Id));
        Assert.Equal("Shatha's found phone", wardInbox.Single(c => c.Id == aboutPhone.Id).Post.ItemName);
        Assert.Equal("Ward's lost wallet", wardInbox.Single(c => c.Id == aboutWallet.Id).Post.ItemName);
        Assert.Equal(1, wardInbox.Single(c => c.Id == aboutWallet.Id).UnreadCount);
        Assert.Equal(0, wardInbox.Single(c => c.Id == aboutPhone.Id).UnreadCount);
    }

    [Fact]
    public async Task Strangers_see_none_of_it()
    {
        string shatha = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        string ward = await NewUserAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);
        await _client.SendMessageOkAsync(ward, chat.Id, "hello");

        var ahmadInbox = await _client.InboxAsync(await NewUserAsync());

        Assert.Empty(ahmadInbox.Items);
        Assert.Equal(0, ahmadInbox.TotalCount);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
}
