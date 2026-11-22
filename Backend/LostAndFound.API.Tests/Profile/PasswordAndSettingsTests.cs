using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Profile;

public sealed class PasswordAndSettingsTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private const string Password = ApiClientExtensions.ValidPassword;
    private const string NewPassword = "N3wPassword";

    private readonly HttpClient _client = factory.CreateClient();

    // ---------- Password ----------

    [Fact]
    public async Task Changing_the_password_replaces_the_old_one()
    {
        (string token, string email, _) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.ChangePasswordAsync(token, Password, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Wrong_current_password_is_rejected_and_nothing_changes()
    {
        (string token, string email, _) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.ChangePasswordAsync(token, "Wr0ngPassword", NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("CurrentPassword", out _));
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Wrong_current_passwords_lock_the_account()
    {
        (string token, string email, _) = await _client.NewAccountAsync();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await _client.ChangePasswordAsync(token, "Wr0ngPassword", NewPassword);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(email, Password)).StatusCode);
    }

    [Theory]
    [InlineData("weakpassword", null)]
    [InlineData("N3wPassword", "Different1Password")]
    [InlineData("Sh0rt", null)]
    public async Task Weak_or_unconfirmed_new_password_is_rejected(string newPassword, string? confirm)
    {
        (string token, string email, _) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.ChangePasswordAsync(token, Password, newPassword, confirm);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Changing_the_password_cancels_an_earlier_reset_link()
    {
        (string token, string email, _) = await _client.NewAccountAsync();
        await _client.ForgotPasswordAsync(email);
        string resetToken = factory.Emails.ResetTokenFromLastEmail(email)!;

        await _client.ChangePasswordAsync(token, Password, NewPassword);
        HttpResponseMessage reset = await _client.ResetPasswordAsync(email, resetToken, "An0therPassword");

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, NewPassword)).StatusCode);
    }

    // ---------- Settings ----------

    [Fact]
    public async Task New_accounts_have_arabic_and_every_notification_on()
    {
        (string token, _, _) = await _client.NewAccountAsync();

        UserSettings settings = await _client.SettingsAsync(token);

        Assert.Equal(new UserSettings
        {
            Language = PreferredLanguage.Arabic,
            NotifyOnNewMessage = true,
            NotifyOnSimilarItem = true,
            NotifyOnPostUpdate = true
        }, settings);
    }

    [Fact]
    public async Task Settings_are_saved_and_returned()
    {
        (string token, _, _) = await _client.NewAccountAsync();
        var wanted = new UserSettings
        {
            Language = PreferredLanguage.English,
            NotifyOnNewMessage = false,
            NotifyOnSimilarItem = true,
            NotifyOnPostUpdate = false
        };

        HttpResponseMessage response = await _client.UpdateSettingsAsync(token, wanted);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(wanted, await response.Content.ReadFromJsonAsync<UserSettings>(ApiClientExtensions.Json));
        Assert.Equal(wanted, await _client.SettingsAsync(token));
    }

    [Fact]
    public async Task Turning_off_message_notifications_stops_them()
    {
        (string shatha, _, _) = await _client.NewAccountAsync();
        PostResponse post = await _client.CreatePostAsync(shatha, PostApi.Body());
        (string ward, _, _) = await _client.NewAccountAsync();
        ConversationResponse chat = await _client.StartChatOkAsync(ward, post.Id);
        await _client.UpdateSettingsAsync(shatha, new
        {
            language = "Arabic",
            notifyOnNewMessage = false,
            notifyOnSimilarItem = true,
            notifyOnPostUpdate = true
        });

        await _client.SendMessageOkAsync(ward, chat.Id, "hello");

        Assert.Equal(0, await _client.UnreadCountAsync(shatha));
    }

    [Fact]
    public async Task Each_user_only_changes_their_own_settings()
    {
        (string first, _, _) = await _client.NewAccountAsync();
        (string second, _, _) = await _client.NewAccountAsync();

        await _client.UpdateSettingsAsync(first, new { language = "English", notifyOnNewMessage = false, notifyOnSimilarItem = false, notifyOnPostUpdate = false });

        Assert.Equal(PreferredLanguage.Arabic, (await _client.SettingsAsync(second)).Language);
        Assert.True((await _client.SettingsAsync(second)).NotifyOnNewMessage);
    }

    [Theory]
    [InlineData("""{ "language": "French", "notifyOnNewMessage": true, "notifyOnSimilarItem": true, "notifyOnPostUpdate": true }""")]
    [InlineData("""{ "language": "English", "notifyOnSimilarItem": true, "notifyOnPostUpdate": true }""")]
    [InlineData("""{ "language": "English", "notifyOnNewMessage": "yes", "notifyOnSimilarItem": true, "notifyOnPostUpdate": true }""")]
    public async Task Invalid_settings_return_400(string json)
    {
        (string token, _, _) = await _client.NewAccountAsync();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/users/me/settings")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await _client.SettingsAsync(token)).NotifyOnNewMessage);
    }
}
