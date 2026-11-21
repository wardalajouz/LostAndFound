using System.Net;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Notifications;

/// <summary>The notifications page: listing, the unread badge, and marking as read.</summary>
public sealed class NotificationCenterTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_is_newest_first_paged_and_filterable_by_unread()
    {
        (string shatha, List<Guid> chats) = await OwnerWithNotificationsAsync(count: 3);
        await _client.MarkReadAsync(shatha, chats[1]);

        var all = await _client.NotificationsAsync(shatha);
        var page2 = await _client.NotificationsAsync(shatha, "pageSize=2&page=2");
        var unread = await _client.NotificationsAsync(shatha, "unreadOnly=true");

        Assert.Equal(3, all.TotalCount);
        Assert.Equal([chats[2], chats[1], chats[0]], all.Items.Select(n => n.ConversationId!.Value));
        Assert.Equal(chats[0], Assert.Single(page2.Items).ConversationId);
        Assert.Equal(2, unread.TotalCount);
        Assert.All(unread.Items, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task Unread_count_matches_the_list()
    {
        (string shatha, _) = await OwnerWithNotificationsAsync(count: 2);

        Assert.Equal(2, await _client.UnreadCountAsync(shatha));
        Assert.Equal(0, await _client.UnreadCountAsync(await NewUserAsync()));
    }

    [Fact]
    public async Task Mark_one_as_read()
    {
        (string shatha, _) = await OwnerWithNotificationsAsync(count: 2);
        Guid first = (await _client.NotificationsAsync(shatha)).Items[0].Id;

        HttpResponseMessage response = await _client.MarkNotificationReadAsync(shatha, first);
        HttpResponseMessage again = await _client.MarkNotificationReadAsync(shatha, first);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(1, await _client.UnreadCountAsync(shatha));
        Assert.True((await _client.NotificationsAsync(shatha)).Items.Single(n => n.Id == first).IsRead);
    }

    [Fact]
    public async Task Someone_elses_or_an_unknown_notification_is_404_and_unchanged()
    {
        (string shatha, _) = await OwnerWithNotificationsAsync(count: 1);
        Guid hers = (await _client.NotificationsAsync(shatha)).Items[0].Id;
        string ahmad = await NewUserAsync();

        HttpResponseMessage stranger = await _client.MarkNotificationReadAsync(ahmad, hers);
        HttpResponseMessage unknown = await _client.MarkNotificationReadAsync(shatha, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, stranger.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(1, await _client.UnreadCountAsync(shatha));
    }

    [Fact]
    public async Task Read_all_marks_only_my_notifications()
    {
        (string shatha, List<Guid> chats) = await OwnerWithNotificationsAsync(count: 2);
        (string other, _) = await OwnerWithNotificationsAsync(count: 1);

        HttpResponseMessage response = await _client.MarkAllNotificationsReadAsync(shatha);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await _client.UnreadCountAsync(shatha));
        Assert.Equal(1, await _client.UnreadCountAsync(other));
        Assert.Equal(2, (await _client.NotificationsAsync(shatha)).TotalCount);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.MarkAllNotificationsReadAsync(shatha)).StatusCode);
    }

    [Fact]
    public async Task Users_never_see_each_others_notifications()
    {
        (string shatha, _) = await OwnerWithNotificationsAsync(count: 2);

        var ahmad = await _client.NotificationsAsync(await NewUserAsync());

        Assert.Empty(ahmad.Items);
        Assert.Equal(0, ahmad.TotalCount);
        Assert.Equal(2, (await _client.NotificationsAsync(shatha)).TotalCount);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=51")]
    [InlineData("page=0")]
    [InlineData("unreadOnly=maybe")]
    public async Task Invalid_query_values_return_400(string query)
    {
        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Get, $"/api/notifications?{query}", await NewUserAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

    /// <summary>An owner with one post per notification, each messaged by a different user, in order.</summary>
    private async Task<(string Owner, List<Guid> Chats)> OwnerWithNotificationsAsync(int count)
    {
        string shatha = await NewUserAsync();
        var chats = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body(itemName: $"Item {i}"));
            string sender = await NewUserAsync();
            ConversationResponse chat = await _client.StartChatOkAsync(sender, post.Id);
            await _client.SendMessageOkAsync(sender, chat.Id, $"about item {i}");
            chats.Add(chat.Id);
        }

        return (shatha, chats);
    }
}
