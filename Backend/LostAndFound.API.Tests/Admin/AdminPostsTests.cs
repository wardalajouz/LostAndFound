using System.Net;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Admin;

public sealed class AdminPostsTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- Posts table ----------

    [Fact]
    public async Task Table_includes_hidden_and_deleted_posts_with_their_visibility()
    {
        string admin = await _client.AdminTokenAsync();
        string marker = PostApi.Marker();
        (string owner, PostResponse visible) = await NewPostAsync($"{marker} visible");
        PostResponse hidden = await _client.CreatePostAsync(owner, PostApi.Body(itemName: $"{marker} hidden"));
        PostResponse deleted = await _client.CreatePostAsync(owner, PostApi.Body(itemName: $"{marker} deleted"));
        await _client.SetVisibilityAsync(admin, hidden.Id, "Hidden");
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{deleted.Id}", owner);

        PagedResponse<AdminPostResponse> page = await _client.AdminPostsAsync(admin, $"search={marker}");

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(PostVisibility.Visible, page.Items.Single(p => p.Id == visible.Id).Visibility);
        Assert.Equal(PostVisibility.Hidden, page.Items.Single(p => p.Id == hidden.Id).Visibility);
        Assert.Equal(PostVisibility.Deleted, page.Items.Single(p => p.Id == deleted.Id).Visibility);
        Assert.Equal(visible.Publisher.Id, page.Items[0].Publisher.Id);
    }

    [Fact]
    public async Task Every_filter_narrows_the_table()
    {
        string admin = await _client.AdminTokenAsync();
        string marker = PostApi.Marker();
        (string owner, PostResponse lostKeys) = await NewPostAsync($"{marker} a", type: "Lost", category: "Keys");
        PostResponse handedOver = await _client.CreatePostAsync(owner, PostApi.Body(
            itemName: $"{marker} b", type: "Found", category: "Electronics", campusLocationId: PostApi.MainGateLocationId, handedToSecurity: true));
        PostResponse resolved = await _client.CreatePostAsync(owner, PostApi.Body(itemName: $"{marker} c", type: "Found", category: "Bags"));
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{resolved.Id}/resolve", owner);
        PostResponse hidden = await _client.CreatePostAsync(owner, PostApi.Body(itemName: $"{marker} d", type: "Lost", category: "Books"));
        await _client.SetVisibilityAsync(admin, hidden.Id, "Hidden");

        async Task<Guid[]> Ids(string filter) =>
            (await _client.AdminPostsAsync(admin, $"search={marker}&{filter}")).Items.Select(p => p.Id).ToArray();

        Assert.Equal([hidden.Id, lostKeys.Id], await Ids("type=Lost"));
        Assert.Equal([lostKeys.Id], await Ids("category=Keys"));
        Assert.Equal([resolved.Id], await Ids("status=Resolved"));
        Assert.Equal([handedOver.Id], await Ids("handedToSecurity=true"));
        Assert.Equal([handedOver.Id], await Ids($"campusLocationId={PostApi.MainGateLocationId}"));
        Assert.Equal([hidden.Id], await Ids("visibility=Hidden"));
        Assert.Equal([lostKeys.Id, handedOver.Id, resolved.Id, hidden.Id], await Ids("sort=Oldest"));
    }

    [Fact]
    public async Task Table_is_paged()
    {
        string admin = await _client.AdminTokenAsync();
        string marker = PostApi.Marker();
        (string owner, _) = await NewPostAsync($"{marker} 0");
        for (int i = 1; i < 5; i++)
        {
            await _client.CreatePostAsync(owner, PostApi.Body(itemName: $"{marker} {i}"));
        }

        PagedResponse<AdminPostResponse> page = await _client.AdminPostsAsync(admin, $"search={marker}&pageSize=2&page=3");

        Assert.Equal(5, page.TotalCount);
        Assert.Equal($"{marker} 0", Assert.Single(page.Items).ItemName);
    }

    [Theory]
    [InlineData("visibility=Gone")]
    [InlineData("status=Lost")]
    [InlineData("handedToSecurity=maybe")]
    [InlineData("pageSize=51")]
    public async Task Invalid_filters_return_400(string query)
    {
        string admin = await _client.AdminTokenAsync();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Get, $"/api/admin/posts?{query}", admin);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Post details ----------

    [Fact]
    public async Task Details_include_verification_details_and_work_for_hidden_and_deleted_posts()
    {
        string admin = await _client.AdminTokenAsync();
        (string owner, PostResponse post) = await NewPostAsync(verificationDetails: "Wallpaper is a cat");
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}", owner);

        AdminPostDetailsResponse details = await _client.AdminPostAsync(admin, post.Id);

        Assert.Equal(post.Id, details.Post.Id);
        Assert.Equal("Wallpaper is a cat", details.VerificationDetails);
        Assert.Equal(PostVisibility.Deleted, details.Visibility);
        Assert.Null(details.ResolvedAt);
        Assert.Empty(details.ModerationHistory);
    }

    [Fact]
    public async Task Unknown_post_returns_404()
    {
        string admin = await _client.AdminTokenAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(HttpMethod.Get, $"/api/admin/posts/{Guid.NewGuid()}", admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SetVisibilityAsync(admin, Guid.NewGuid(), "Hidden")).StatusCode);
    }

    // ---------- Visibility ----------

    [Fact]
    public async Task Hiding_removes_the_post_from_public_pages_logs_it_and_tells_the_owner()
    {
        string admin = await _client.AdminTokenAsync();
        (string owner, PostResponse post) = await NewPostAsync("Black wallet");

        HttpResponseMessage response = await _client.SetVisibilityAsync(admin, post.Id, "Hidden", "  Duplicate report  ");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/posts/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, $"/api/users/me/posts/{post.Id}", owner)).StatusCode);

        ModerationLogEntryResponse entry = Assert.Single((await _client.AdminPostAsync(admin, post.Id)).ModerationHistory);
        Assert.Equal(PostVisibility.Hidden, entry.Visibility);
        Assert.Equal("Duplicate report", entry.Reason);
        Assert.Equal("Security Admin", entry.Admin.FullName);

        NotificationResponse notification = Assert.Single((await _client.NotificationsAsync(owner, language: "en")).Items);
        Assert.Equal(NotificationType.PostModerated, notification.Type);
        Assert.Equal(post.Id, notification.PostId);
        Assert.Contains("\"Black wallet\"", notification.Message);
    }

    [Fact]
    public async Task Restoring_brings_the_post_back_and_history_is_newest_first()
    {
        string admin = await _client.AdminTokenAsync();
        (_, PostResponse post) = await NewPostAsync();
        await _client.SetVisibilityAsync(admin, post.Id, "Deleted", "Offensive photo");

        HttpResponseMessage response = await _client.SetVisibilityAsync(admin, post.Id, "Visible", "Photo replaced");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/posts/{post.Id}")).StatusCode);
        var history = (await _client.AdminPostAsync(admin, post.Id)).ModerationHistory;
        Assert.Equal([PostVisibility.Visible, PostVisibility.Deleted], history.Select(h => h.Visibility));
        Assert.Equal(["Photo replaced", "Offensive photo"], history.Select(h => h.Reason));
    }

    [Fact]
    public async Task Setting_the_same_visibility_again_is_409_and_not_logged()
    {
        string admin = await _client.AdminTokenAsync();
        (_, PostResponse post) = await NewPostAsync();

        HttpResponseMessage alreadyVisible = await _client.SetVisibilityAsync(admin, post.Id, "Visible");
        await _client.SetVisibilityAsync(admin, post.Id, "Hidden");
        HttpResponseMessage alreadyHidden = await _client.SetVisibilityAsync(admin, post.Id, "Hidden");

        Assert.Equal(HttpStatusCode.Conflict, alreadyVisible.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, alreadyHidden.StatusCode);
        Assert.Single((await _client.AdminPostAsync(admin, post.Id)).ModerationHistory);
    }

    [Fact]
    public async Task Posts_of_a_deleted_account_cannot_be_shown_again()
    {
        string admin = await _client.AdminTokenAsync();
        (string owner, PostResponse post) = await NewPostAsync();
        await _client.DeleteAccountAsync(owner, ApiClientExtensions.ValidPassword);

        HttpResponseMessage restore = await _client.SetVisibilityAsync(admin, post.Id, "Visible");
        HttpResponseMessage hide = await _client.SetVisibilityAsync(admin, post.Id, "Hidden");

        Assert.Equal(HttpStatusCode.Conflict, restore.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, hide.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/posts/{post.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("ok")]
    [InlineData("")]
    [InlineData("    x    ")]
    public async Task A_real_reason_is_required(string reason)
    {
        string admin = await _client.AdminTokenAsync();
        (_, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await _client.SetVisibilityAsync(admin, post.Id, "Hidden", reason);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PostVisibility.Visible, (await _client.AdminPostAsync(admin, post.Id)).Visibility);
    }

    [Fact]
    public async Task Unknown_visibility_value_is_400()
    {
        string admin = await _client.AdminTokenAsync();
        (_, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await _client.SetVisibilityAsync(admin, post.Id, "Archived");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Owner_who_turned_off_post_updates_is_not_notified()
    {
        string admin = await _client.AdminTokenAsync();
        (string owner, PostResponse post) = await NewPostAsync();
        await _client.UpdateSettingsAsync(owner, new { language = "Arabic", notifyOnNewMessage = true, notifyOnSimilarItem = true, notifyOnPostUpdate = false });

        await _client.SetVisibilityAsync(admin, post.Id, "Hidden");

        Assert.Equal(0, await _client.UnreadCountAsync(owner));
    }

    [Fact]
    public async Task Failed_save_changes_nothing()
    {
        string admin = await _client.AdminTokenAsync();
        (string owner, PostResponse post) = await NewPostAsync();
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.SetVisibilityAsync(admin, post.Id, "Hidden");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AdminPostDetailsResponse details = await _client.AdminPostAsync(admin, post.Id);
        Assert.Equal(PostVisibility.Visible, details.Visibility);
        Assert.Empty(details.ModerationHistory);
        Assert.Equal(0, await _client.UnreadCountAsync(owner));
    }

    private async Task<(string Owner, PostResponse Post)> NewPostAsync(
        string itemName = "Samsung Galaxy S23", string type = "Found", string category = "Electronics", string? verificationDetails = null)
    {
        string owner = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(owner, PostApi.Body(
            itemName: itemName, type: type, category: category, verificationDetails: type == "Found" ? verificationDetails : null));
        return (owner, post);
    }
}
