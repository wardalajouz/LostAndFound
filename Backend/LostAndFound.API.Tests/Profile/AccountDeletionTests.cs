using System.Net;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Services.Profile;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Profile;

public sealed class AccountDeletionTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private const string Password = ApiClientExtensions.ValidPassword;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Deletion_needs_the_right_password()
    {
        (string token, string email, _) = await _client.NewAccountAsync();

        HttpResponseMessage missing = await _client.DeleteAccountAsync(token, password: null);
        HttpResponseMessage wrong = await _client.DeleteAccountAsync(token, "Wr0ngPassword");

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.True((await wrong.ReadJsonAsync()).GetProperty("errors").TryGetProperty("Password", out _));
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, Password)).StatusCode);
        Assert.Equal(email, (await _client.ProfileAsync(token)).Email);
    }

    [Fact]
    public async Task Deleted_account_is_signed_out_everywhere_and_cannot_log_in()
    {
        (string token, string email, _) = await _client.NewAccountAsync();
        string otherSession = await _client.LoginAndGetTokenAsync(email, Password);

        HttpResponseMessage response = await _client.DeleteAccountAsync(token, Password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(HttpMethod.Get, "/api/users/me/posts", otherSession)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Personal_data_is_removed_but_the_row_is_kept()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync("Ward Haddad");
        AvatarResponse avatar = await _client.UploadAvatarOkAsync(token, ImageApi.Jpeg());

        await _client.DeleteAccountAsync(token, Password);

        await factory.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == id);
            Assert.Equal(AccountDeletion.DeletedUserName, user.FullName);
            Assert.Null(user.Email);
            Assert.Null(user.NormalizedEmail);
            Assert.Null(user.PasswordHash);
            Assert.Null(user.AvatarUrl);
            Assert.NotNull(user.DeletedAt);
            Assert.DoesNotContain("@", user.UserName);
        });
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(avatar.Url)).StatusCode);
    }

    [Fact]
    public async Task The_email_can_be_used_for_a_new_account_afterwards()
    {
        (string token, string email, _) = await _client.NewAccountAsync();
        await _client.DeleteAccountAsync(token, Password);

        HttpResponseMessage register = await _client.RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
    }

    [Fact]
    public async Task Posts_are_hidden_and_notifications_deleted()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();
        string marker = PostApi.Marker();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body(itemName: marker));
        (string ward, _, _) = await _client.NewAccountAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);
        await _client.SendMessageOkAsync(ward, chat.Id, "hi");

        await _client.DeleteAccountAsync(token, Password);

        Assert.Empty((await _client.BrowseAsync($"search={marker}")).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/posts/{post.Id}")).StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(PostVisibility.Deleted, (await db.Posts.SingleAsync(p => p.Id == post.Id)).Visibility);
            Assert.Equal(0, await db.Notifications.CountAsync(n => n.UserId == id));
        });
    }

    [Fact]
    public async Task The_other_person_keeps_the_chat_showing_deleted_user()
    {
        (string shatha, _, _) = await _client.NewAccountAsync("Shatha");
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        (string ward, _, _) = await _client.NewAccountAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);
        await _client.SendMessageOkAsync(ward, chat.Id, "is it mine?");
        await _client.SendMessageOkAsync(shatha, chat.Id, "what colour is it?");

        await _client.DeleteAccountAsync(shatha, Password);

        ConversationResponse header = await _client.ChatAsync(ward, chat.Id);
        Assert.Equal(AccountDeletion.DeletedUserName, header.OtherParticipant.FullName);
        Assert.Null(header.OtherParticipant.AvatarUrl);
        Assert.Equal(2, (await _client.MessagesAsync(ward, chat.Id)).TotalCount);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendMessageAsync(ward, chat.Id, "hello?")).StatusCode);
    }

    [Fact]
    public async Task Support_tickets_lose_the_name_and_email()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();
        await _client.SendAsync(HttpMethod.Post, "/api/support-tickets", token, new
        {
            fullName = "Ward Haddad",
            email = "ward.private@findly.test",
            subject = "Login problem",
            message = "I cannot log in from my phone."
        });

        await _client.DeleteAccountAsync(token, Password);

        await factory.WithDbAsync(async db =>
        {
            var ticket = await db.SupportTickets.SingleAsync(t => t.UserId == id);
            Assert.Equal(AccountDeletion.DeletedUserName, ticket.FullName);
            Assert.Equal(string.Empty, ticket.Email);
        });
    }

    [Fact]
    public async Task Other_accounts_are_untouched()
    {
        (string token, _, _) = await _client.NewAccountAsync();
        (string other, string otherEmail, _) = await _client.NewAccountAsync("Other Person");
        PostResponse otherPost = await _client.CreatePostAsync(other, PostApi.Body());

        await _client.DeleteAccountAsync(token, Password);

        Assert.Equal("Other Person", (await _client.ProfileAsync(other)).FullName);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(otherEmail, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/posts/{otherPost.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_failure_part_way_leaves_the_account_exactly_as_it_was()
    {
        (string token, string email, Guid id) = await _client.NewAccountAsync();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());
        PostVisibility? visibilityWhenSaveFailed = null;
        factory.SaveHooks.BeforeNextSave(async context => visibilityWhenSaveFailed = await context.Set<LostAndFound.Api.Data.Entities.Post>()
            .AsNoTracking().Where(p => p.Id == post.Id).Select(p => p.Visibility).SingleAsync());
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.DeleteAccountAsync(token, Password);

        // The failing save came after the posts were hidden inside the transaction, and the rollback undid that.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(PostVisibility.Deleted, visibilityWhenSaveFailed);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/posts/{post.Id}")).StatusCode);
        Assert.Equal(email, (await _client.ProfileAsync(token)).Email);
        await factory.WithDbAsync(async db => Assert.Null((await db.Users.SingleAsync(u => u.Id == id)).DeletedAt));
    }
}

public sealed class AdminAccountDeletionTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    [Fact]
    public async Task Deleting_an_admin_account_removes_its_role()
    {
        HttpClient client = factory.CreateClient();
        string token = await client.LoginAndGetTokenAsync(AdminApiFactory.AdminEmail, AdminApiFactory.AdminPassword);
        Guid adminId = (await client.ProfileAsync(token)).Id;

        HttpResponseMessage response = await client.DeleteAccountAsync(token, AdminApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await factory.WithDbAsync(async db => Assert.False(await db.UserRoles.AnyAsync(r => r.UserId == adminId)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(HttpMethod.Get, "/api/admin/stats", token)).StatusCode);
    }
}
