using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using LostAndFound.Api.Email;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Api.Tests.Auth;

public sealed class PasswordResetTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private const string NewPassword = "N3wPassword";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Full_flow_changes_the_password()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        HttpResponseMessage forgot = await ForgotAsync(email);
        Assert.Equal(HttpStatusCode.Accepted, forgot.StatusCode);

        (string linkEmail, string token) = ReadResetLink(factory.Emails.SentTo(email).Single());
        Assert.Equal(email, linkEmail);

        HttpResponseMessage reset = await ResetAsync(linkEmail, token, NewPassword);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(email, ApiClientExtensions.ValidPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Unknown_email_returns_202_and_sends_nothing()
    {
        string email = ApiClientExtensions.UniqueEmail();

        HttpResponseMessage response = await ForgotAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task Disabled_account_returns_202_and_sends_nothing()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(u => u.Email == email)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, AccountStatus.Disabled));
        }

        HttpResponseMessage response = await ForgotAsync(email);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.Emails.SentTo(email));
    }

    [Fact]
    public async Task A_reset_link_works_only_once()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);
        await ForgotAsync(email);
        (_, string token) = ReadResetLink(factory.Emails.SentTo(email).Single());

        Assert.Equal(HttpStatusCode.NoContent, (await ResetAsync(email, token, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(email, token, "An0therPassword")).StatusCode);
    }

    [Fact]
    public async Task A_token_for_one_account_cannot_reset_another()
    {
        string victim = ApiClientExtensions.UniqueEmail();
        string attacker = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(victim);
        await _client.RegisterAndGetTokenAsync(attacker);
        await ForgotAsync(attacker);
        (_, string attackerToken) = ReadResetLink(factory.Emails.SentTo(attacker).Single());

        HttpResponseMessage response = await ResetAsync(victim, attackerToken, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(victim, ApiClientExtensions.ValidPassword)).StatusCode);
    }

    [Theory]
    [InlineData("not-a-real-token")]
    [InlineData("%%%")]
    public async Task Invalid_token_returns_400_with_a_token_error(string token)
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        HttpResponseMessage response = await ResetAsync(email, token, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("Token", out _));
    }

    [Fact]
    public async Task Weak_new_password_returns_400_and_keeps_the_old_password()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);
        await ForgotAsync(email);
        (_, string token) = ReadResetLink(factory.Emails.SentTo(email).Single());

        HttpResponseMessage response = await ResetAsync(email, token, "weakpassword");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("NewPassword", out _));
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, ApiClientExtensions.ValidPassword)).StatusCode);
    }

    [Fact]
    public async Task Reset_unlocks_a_locked_account()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            await _client.LoginAsync(email, "Wr0ngPassword");
        }

        await ForgotAsync(email);
        (_, string token) = ReadResetLink(factory.Emails.SentTo(email).Single());
        await ResetAsync(email, token, NewPassword);

        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, NewPassword)).StatusCode);
    }

    private Task<HttpResponseMessage> ForgotAsync(string email) =>
        _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

    private Task<HttpResponseMessage> ResetAsync(string email, string token, string newPassword) =>
        _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword,
            confirmNewPassword = newPassword
        });

    private static (string Email, string Token) ReadResetLink(EmailMessage message)
    {
        string link = message.TextBody.Split('\n').Select(line => line.Trim())
            .First(line => line.StartsWith("http", StringComparison.Ordinal));
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(new Uri(link).Query);
        return (query["email"].ToString(), query["token"].ToString());
    }
}
