using System.Net;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Notifications;

/// <summary>Shatha owns a post; Ward and Ahmad message her about it.</summary>
public sealed class NotificationTriggerTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- New message ----------

    [Fact]
    public async Task A_message_notifies_the_other_person_only()
    {
        Chat chat = await NewChatAsync(itemName: "Black wallet");

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "Is it mine?");

        NotificationResponse notification = Assert.Single((await _client.NotificationsAsync(chat.Shatha, language: "en")).Items);
        Assert.Equal(NotificationType.NewMessage, notification.Type);
        Assert.Equal(chat.Id, notification.ConversationId);
        Assert.Equal(chat.PostId, notification.PostId);
        Assert.False(notification.IsRead);
        Assert.Equal("New message", notification.Title);
        Assert.Equal("Ward Alajouz sent you a message about \"Black wallet\".", notification.Message);
        Assert.Empty((await _client.NotificationsAsync(chat.Ward)).Items);
    }

    [Fact]
    public async Task Each_side_sees_the_other_persons_name()
    {
        string shatha = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail(), "Shatha Nasser");
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body(itemName: "Black wallet"));
        string ward = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail(), "Ward Haddad");
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);

        await _client.SendMessageOkAsync(ward, chat.Id, "Is it mine?");
        await _client.SendMessageOkAsync(shatha, chat.Id, "What is inside?");

        Assert.StartsWith("Ward Haddad ", Assert.Single((await _client.NotificationsAsync(shatha, language: "en")).Items).Message);
        Assert.StartsWith("Shatha Nasser ", Assert.Single((await _client.NotificationsAsync(ward, language: "en")).Items).Message);
    }

    [Fact]
    public async Task Message_and_notification_are_saved_in_one_database_save()
    {
        Chat chat = await NewChatAsync();
        int before = factory.SaveHooks.SaveCount;

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hi");

        Assert.Equal(before + 1, factory.SaveHooks.SaveCount);
        Assert.Equal(1, await CountNotificationsAsync(chat.ShathaId));
    }

    [Fact]
    public async Task Resolve_and_its_notifications_are_saved_in_one_database_save()
    {
        Chat chat = await NewChatAsync();
        int before = factory.SaveHooks.SaveCount;

        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{chat.PostId}/resolve", chat.Shatha);

        Assert.Equal(before + 1, factory.SaveHooks.SaveCount);
        Assert.Equal(1, await CountNotificationsAsync(chat.WardId));
    }

    [Fact]
    public async Task Replies_notify_the_starter()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hello");

        await _client.SendMessageOkAsync(chat.Shatha, chat.Id, "What is the wallpaper?");

        NotificationResponse notification = Assert.Single((await _client.NotificationsAsync(chat.Ward)).Items);
        Assert.Equal(chat.Id, notification.ConversationId);
    }

    [Fact]
    public async Task Text_follows_the_requested_language()
    {
        Chat chat = await NewChatAsync(itemName: "Black wallet");
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hi");

        NotificationResponse arabic = Assert.Single((await _client.NotificationsAsync(chat.Shatha)).Items);

        Assert.Equal("رسالة جديدة", arabic.Title);
        Assert.Contains("\"Black wallet\"", arabic.Message);
    }

    [Fact]
    public async Task Several_messages_in_a_row_make_one_notification_that_moves_to_the_top()
    {
        Chat chat = await NewChatAsync();
        Chat other = await NewChatAsync(shatha: chat.Shatha);
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "one");
        await _client.SendMessageOkAsync(other.Ward, other.Id, "from someone else");

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "two");
        MessageResponse last = await _client.SendMessageOkAsync(chat.Ward, chat.Id, "three");

        var items = (await _client.NotificationsAsync(chat.Shatha)).Items;
        Assert.Equal([chat.Id, other.Id], items.Select(n => n.ConversationId!.Value));
        Assert.Equal(last.SentAt, items[0].CreatedAt);
        Assert.Equal(2, await _client.UnreadCountAsync(chat.Shatha));
    }

    [Fact]
    public async Task Reading_the_chat_clears_its_notification_and_the_next_message_makes_a_new_one()
    {
        Chat chat = await NewChatAsync();
        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "one");

        await _client.MarkReadAsync(chat.Shatha, chat.Id);

        Assert.Equal(0, await _client.UnreadCountAsync(chat.Shatha));
        Assert.True(Assert.Single((await _client.NotificationsAsync(chat.Shatha)).Items).IsRead);

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "two");

        var items = (await _client.NotificationsAsync(chat.Shatha)).Items;
        Assert.Equal(2, items.Count);
        Assert.Equal([false, true], items.Select(n => n.IsRead));
    }

    [Fact]
    public async Task Reading_one_chat_leaves_other_notifications_alone()
    {
        Chat first = await NewChatAsync();
        Chat second = await NewChatAsync(shatha: first.Shatha);
        await _client.SendMessageOkAsync(first.Ward, first.Id, "a");
        await _client.SendMessageOkAsync(second.Ward, second.Id, "b");

        await _client.MarkReadAsync(first.Shatha, first.Id);

        NotificationResponse unread = Assert.Single((await _client.NotificationsAsync(first.Shatha, "unreadOnly=true")).Items);
        Assert.Equal(second.Id, unread.ConversationId);
    }

    [Fact]
    public async Task Recipient_who_turned_off_message_notifications_gets_none()
    {
        Chat chat = await NewChatAsync();
        await SetSettingAsync(chat.ShathaId, onNewMessage: false);

        await _client.SendMessageOkAsync(chat.Ward, chat.Id, "hi");

        Assert.Empty((await _client.NotificationsAsync(chat.Shatha)).Items);
    }

    [Fact]
    public async Task Deleted_account_gets_no_notifications()
    {
        Chat chat = await NewChatAsync();
        await factory.WithDbAsync(db => db.Users.Where(u => u.Id == chat.ShathaId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletedAt, DateTimeOffset.UtcNow)));

        HttpResponseMessage response = await _client.SendMessageAsync(chat.Ward, chat.Id, "hi");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(0, await CountNotificationsAsync(chat.ShathaId));
    }

    [Fact]
    public async Task Failed_send_stores_neither_the_message_nor_the_notification()
    {
        Chat chat = await NewChatAsync();
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.SendMessageAsync(chat.Ward, chat.Id, "hi");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(0, (await _client.MessagesAsync(chat.Ward, chat.Id)).TotalCount);
        Assert.Equal(0, await CountNotificationsAsync(chat.ShathaId));
    }

    // ---------- Post resolved ----------

    [Fact]
    public async Task Resolving_notifies_everyone_who_messaged_about_the_post()
    {
        Chat ward = await NewChatAsync(itemName: "Black wallet");
        string ahmad = await NewUserAsync();
        await _client.StartChatOkAsync(ahmad, ward.PostId);
        string stranger = await NewUserAsync();

        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{ward.PostId}/resolve", ward.Shatha);

        foreach (string token in new[] { ward.Ward, ahmad })
        {
            NotificationResponse n = Assert.Single((await _client.NotificationsAsync(token, language: "en")).Items);
            Assert.Equal(NotificationType.PostResolved, n.Type);
            Assert.Equal(ward.PostId, n.PostId);
            Assert.Null(n.ConversationId);
            Assert.Equal("\"Black wallet\", which you messaged about, has been marked as resolved.", n.Message);
        }

        Assert.Empty((await _client.NotificationsAsync(ward.Shatha)).Items);
        Assert.Empty((await _client.NotificationsAsync(stranger)).Items);
    }

    [Fact]
    public async Task Resolving_twice_does_not_notify_twice()
    {
        Chat chat = await NewChatAsync();

        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{chat.PostId}/resolve", chat.Shatha);
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{chat.PostId}/resolve", chat.Shatha);

        Assert.Single((await _client.NotificationsAsync(chat.Ward)).Items);
    }

    [Fact]
    public async Task Resolving_a_post_nobody_messaged_about_notifies_no_one()
    {
        string shatha = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        int before = await CountAllNotificationsAsync();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{post.Id}/resolve", shatha);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before, await CountAllNotificationsAsync());
    }

    [Fact]
    public async Task Users_who_turned_off_post_updates_are_not_told()
    {
        Chat chat = await NewChatAsync();
        await SetSettingAsync(chat.WardId, onPostUpdate: false);

        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{chat.PostId}/resolve", chat.Shatha);

        Assert.Empty((await _client.NotificationsAsync(chat.Ward)).Items);
    }

    [Fact]
    public async Task Failed_resolve_stores_no_notification_and_leaves_the_post_active()
    {
        Chat chat = await NewChatAsync();
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{chat.PostId}/resolve", chat.Shatha);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(0, await CountNotificationsAsync(chat.WardId));
        Assert.Equal(PostStatus.Active, (await _client.GetAsync<PostResponse>($"/api/posts/{chat.PostId}")).Status);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

    private async Task<Chat> NewChatAsync(string itemName = "Samsung Galaxy S23", string? shatha = null)
    {
        shatha ??= await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body(itemName: itemName));
        string ward = await NewUserAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);
        Guid wardId = (await _client.ChatAsync(shatha, chat.Id)).OtherParticipant.Id;
        return new Chat(chat.Id, post.Id, shatha, post.Publisher.Id, ward, wardId);
    }

    private Task SetSettingAsync(Guid userId, bool? onNewMessage = null, bool? onPostUpdate = null) =>
        factory.WithDbAsync(db => db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s
            .SetProperty(u => u.NotifyOnNewMessage, u => onNewMessage ?? u.NotifyOnNewMessage)
            .SetProperty(u => u.NotifyOnPostUpdate, u => onPostUpdate ?? u.NotifyOnPostUpdate)));

    private async Task<int> CountNotificationsAsync(Guid userId)
    {
        int count = 0;
        await factory.WithDbAsync(async db => count = await db.Notifications.CountAsync(n => n.UserId == userId));
        return count;
    }

    private async Task<int> CountAllNotificationsAsync()
    {
        int count = 0;
        await factory.WithDbAsync(async db => count = await db.Notifications.CountAsync());
        return count;
    }

    private sealed record Chat(Guid Id, Guid PostId, string Shatha, Guid ShathaId, string Ward, Guid WardId);
}
