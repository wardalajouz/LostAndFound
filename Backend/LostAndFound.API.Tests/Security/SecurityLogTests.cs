using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Security;

/// <summary>Security events are logged with account ids, and personal data never reaches the logs.</summary>
public sealed class SecurityLogTests : IAsyncLifetime
{
    private const string Password = ApiClientExtensions.ValidPassword;

    private readonly AdminApiFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Failed_logins_and_lockouts_are_logged_by_account_id()
    {
        (_, string email, Guid id) = await _client.NewAccountAsync();

        for (int i = 0; i < 5; i++)
        {
            await _client.LoginAsync(email, "Wr0ngPassword");
        }

        await _client.LoginAsync(ApiClientExtensions.UniqueEmail(), "Wr0ngPassword");

        Assert.Equal(5, _factory.Logs.WithEventId(1001).Count(e => e.Message.Contains(id.ToString(), StringComparison.Ordinal)));
        Assert.Contains(id.ToString(), Assert.Single(_factory.Logs.WithEventId(1003)).Message);
        Assert.Single(_factory.Logs.WithEventId(1002));
    }

    [Fact]
    public async Task Account_changes_and_admin_actions_are_logged()
    {
        (string token, string email, Guid id) = await _client.NewAccountAsync();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());
        string admin = await _client.AdminTokenAsync();

        await _client.ForgotPasswordAsync(email);
        await _client.ResetPasswordAsync(email, _factory.Emails.ResetTokenFromLastEmail(email)!, "N3wPassword");
        await _client.ChangePasswordAsync(token, "N3wPassword", Password);
        await _client.UpdateProfileAsync(token, ApiClientExtensions.UniqueEmail(), currentPassword: Password);
        await _client.SetVisibilityAsync(admin, post.Id, "Hidden");
        await _client.SetAccountStatusAsync(admin, id, "Disabled");
        await _client.SetAccountStatusAsync(admin, id, "Active");
        await _client.DeleteAccountAsync(token, Password);

        foreach (int eventId in new[] { 1010, 1011, 1012, 1013, 1014 })
        {
            Assert.Contains(id.ToString(), Assert.Single(_factory.Logs.WithEventId(eventId)).Message);
        }

        Assert.Contains("Hidden", Assert.Single(_factory.Logs.WithEventId(1020)).Message);
        Assert.Equal(["Disabled", "Active"], _factory.Logs.WithEventId(1021).Select(e => e.Message.Split(' ')[^1].TrimEnd('.')));
    }

    [Fact]
    public async Task No_email_address_password_or_token_is_ever_logged()
    {
        const string fullName = "Ward PrivateName";
        (string token, string email, _) = await _client.NewAccountAsync(fullName);
        await _client.LoginAsync(email, "Wr0ngSecretGuess");
        await _client.LoginAsync(email, Password);
        await _client.ForgotPasswordAsync(email);
        string resetToken = _factory.Emails.ResetTokenFromLastEmail(email)!;
        await _client.ResetPasswordAsync(email, resetToken, "N3wSecretPassword");
        await _client.DeleteAccountAsync(token, "N3wSecretPassword");

        string everything = string.Join("\n", _factory.Logs.Entries.Select(e => $"{e.Message} {e.Exception}"));

        Assert.DoesNotContain(email, everything, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fullName, everything);
        Assert.DoesNotContain("Wr0ngSecretGuess", everything);
        Assert.DoesNotContain("N3wSecretPassword", everything);
        Assert.DoesNotContain(Password, everything);
        Assert.DoesNotContain(resetToken, everything);
        Assert.DoesNotContain(token, everything);
    }
}
