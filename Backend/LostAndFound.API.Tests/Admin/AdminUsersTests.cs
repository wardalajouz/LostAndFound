using System.Net;
using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Admin;

public sealed class AdminUsersTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private const string Password = ApiClientExtensions.ValidPassword;

    private readonly HttpClient _client = factory.CreateClient();

    // ---------- Users table ----------

    [Fact]
    public async Task Table_shows_role_status_and_post_count_and_leaves_out_deleted_accounts()
    {
        string admin = await _client.AdminTokenAsync();
        string marker = PostApi.Marker();
        (string ward, _, Guid wardId) = await _client.NewAccountAsync($"{marker} Ward");
        await _client.CreatePostAsync(ward, PostApi.Body());
        var deletedPost = await _client.CreatePostAsync(ward, PostApi.Body());
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{deletedPost.Id}", ward);
        (string gone, _, _) = await _client.NewAccountAsync($"{marker} Gone");
        await _client.DeleteAccountAsync(gone, Password);

        PagedResponse<AdminUserResponse> page = await _client.AdminUsersAsync(admin, $"search={marker}");
        PagedResponse<AdminUserResponse> deletedNames = await _client.AdminUsersAsync(admin, "search=Deleted user");

        Assert.Empty(deletedNames.Items);
        AdminUserResponse row = Assert.Single(page.Items);
        Assert.Equal(wardId, row.Id);
        Assert.Equal(UserRole.User, row.Role);
        Assert.Equal(AccountStatus.Active, row.Status);
        Assert.Equal(UserType.Student, row.UserType);
        Assert.Equal(1, row.PostCount);
    }

    [Fact]
    public async Task Admin_accounts_show_the_admin_role()
    {
        string admin = await _client.AdminTokenAsync();

        PagedResponse<AdminUserResponse> page = await _client.AdminUsersAsync(admin, $"search={AdminApiFactory.AdminEmail}");

        Assert.Equal(UserRole.Admin, Assert.Single(page.Items).Role);
    }

    [Fact]
    public async Task Search_matches_name_or_email_ignoring_case_and_treats_wildcards_literally()
    {
        string admin = await _client.AdminTokenAsync();
        string marker = PostApi.Marker();
        (_, string email, Guid byEmail) = await _client.NewAccountAsync($"Someone {marker}_x");
        (_, _, Guid other) = await _client.NewAccountAsync($"Someone {marker}ZZx");

        Guid[] literal = (await _client.AdminUsersAsync(admin, $"search={Uri.EscapeDataString($"{marker}_x")}")).Items.Select(u => u.Id).ToArray();
        Guid[] emailMatch = (await _client.AdminUsersAsync(admin, $"search={email.ToUpperInvariant()}")).Items.Select(u => u.Id).ToArray();

        Assert.Equal([byEmail], literal);
        Assert.Equal([byEmail], emailMatch);
        Assert.DoesNotContain(other, literal);
    }

    [Fact]
    public async Task Filters_by_status_and_user_type_and_pages_newest_first()
    {
        string admin = await _client.AdminTokenAsync();
        string marker = PostApi.Marker();
        var ids = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            ids.Add((await _client.NewAccountAsync($"{marker} {i}")).Id);
        }

        await _client.SetAccountStatusAsync(admin, ids[1], "Disabled");
        await factory.WithDbAsync(db => db.Users.Where(u => u.Id == ids[2])
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.UserType, UserType.Visitor)));

        var all = await _client.AdminUsersAsync(admin, $"search={marker}");
        var disabled = await _client.AdminUsersAsync(admin, $"search={marker}&status=Disabled");
        var visitors = await _client.AdminUsersAsync(admin, $"search={marker}&userType=Visitor");
        var secondPage = await _client.AdminUsersAsync(admin, $"search={marker}&pageSize=2&page=2");

        Assert.Equal([ids[2], ids[1], ids[0]], all.Items.Select(u => u.Id));
        Assert.Equal([ids[1]], disabled.Items.Select(u => u.Id));
        Assert.Equal([ids[2]], visitors.Items.Select(u => u.Id));
        Assert.Equal([ids[0]], secondPage.Items.Select(u => u.Id));
        Assert.Equal(3, secondPage.TotalCount);
    }

    // ---------- Disable and reactivate ----------

    [Fact]
    public async Task Disabling_cuts_the_user_off_at_once_and_is_logged()
    {
        string admin = await _client.AdminTokenAsync();
        (string token, string email, Guid id) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, id, "Disabled", "  Posting spam  ");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.LoginAsync(email, Password)).StatusCode);
        Assert.Equal(AccountStatus.Disabled, (await _client.AdminUsersAsync(admin, $"search={email}")).Items.Single().Status);
        await factory.WithDbAsync(async db =>
        {
            var log = await db.UserModerationLogs.SingleAsync(l => l.UserId == id);
            Assert.Equal(AccountStatus.Disabled, log.Status);
            Assert.Equal("Posting spam", log.Reason);
            Assert.Equal(await db.Users.Where(u => u.Email == AdminApiFactory.AdminEmail).Select(u => u.Id).SingleAsync(), log.AdminId);
        });
    }

    [Fact]
    public async Task Reactivating_lets_the_user_back_in()
    {
        string admin = await _client.AdminTokenAsync();
        (string token, string email, Guid id) = await _client.NewAccountAsync();
        await _client.SetAccountStatusAsync(admin, id, "Disabled");

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, id, "Active", "Appeal accepted");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, Password)).StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(2, await db.UserModerationLogs.CountAsync(l => l.UserId == id)));
    }

    [Fact]
    public async Task Same_status_again_is_409_and_not_logged()
    {
        string admin = await _client.AdminTokenAsync();
        (_, _, Guid id) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, id, "Active");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(0, await db.UserModerationLogs.CountAsync(l => l.UserId == id)));
    }

    [Fact]
    public async Task An_admin_cannot_change_their_own_status()
    {
        string admin = await _client.AdminTokenAsync();
        Guid adminId = (await _client.ProfileAsync(admin)).Id;

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, adminId, "Disabled");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/admin/stats", admin)).StatusCode);
    }

    [Fact]
    public async Task An_admin_cannot_disable_another_admin()
    {
        string admin = await _client.AdminTokenAsync();
        (_, string otherEmail, Guid otherAdmin) = await _client.NewAccountAsync();
        await MakeAdminAsync(otherAdmin);
        string otherToken = await _client.LoginAndGetTokenAsync(otherEmail, Password); // roles are read at login

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, otherAdmin, "Disabled");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/admin/stats", otherToken)).StatusCode);
    }

    [Fact]
    public async Task Deleted_or_unknown_accounts_are_404()
    {
        string admin = await _client.AdminTokenAsync();
        (string token, _, Guid id) = await _client.NewAccountAsync();
        await _client.DeleteAccountAsync(token, Password);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.SetAccountStatusAsync(admin, id, "Disabled")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SetAccountStatusAsync(admin, Guid.NewGuid(), "Disabled")).StatusCode);
    }

    [Theory]
    [InlineData("Disabled", "  ")]
    [InlineData("Disabled", "no")]
    [InlineData("Disabled", "    x    ")]
    [InlineData("Banned", "Spam reports")]
    public async Task Invalid_status_or_reason_is_400(string status, string reason)
    {
        string admin = await _client.AdminTokenAsync();
        (string token, _, Guid id) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, id, status, reason);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
    }

    [Fact]
    public async Task Failed_save_leaves_the_account_active()
    {
        string admin = await _client.AdminTokenAsync();
        (string token, _, Guid id) = await _client.NewAccountAsync();
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.SetAccountStatusAsync(admin, id, "Disabled");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(0, await db.UserModerationLogs.CountAsync(l => l.UserId == id)));
    }

    private Task MakeAdminAsync(Guid userId) =>
        factory.WithDbAsync(async db =>
        {
            Guid roleId = await db.Roles.Where(r => r.Name == Roles.Admin).Select(r => r.Id).SingleAsync();
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
            await db.SaveChangesAsync();
        });
}
