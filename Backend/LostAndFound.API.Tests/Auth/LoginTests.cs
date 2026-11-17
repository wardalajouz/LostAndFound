using System.Net;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LostAndFound.Api.Tests.Auth;

public sealed class LoginTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Correct_credentials_return_200_with_a_token()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        HttpResponseMessage response = await _client.LoginAsync(email, ApiClientExtensions.ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Email_is_matched_case_insensitively_and_trimmed()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        HttpResponseMessage response = await _client.LoginAsync($"  {email.ToUpperInvariant()} ", ApiClientExtensions.ValidPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_return_the_same_401()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        HttpResponseMessage wrongPassword = await _client.LoginAsync(email, "Wr0ngPassword");
        HttpResponseMessage unknownEmail = await _client.LoginAsync(ApiClientExtensions.UniqueEmail(), "Wr0ngPassword");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(
            (await wrongPassword.ReadJsonAsync()).GetProperty("detail").GetString(),
            (await unknownEmail.ReadJsonAsync()).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Five_failed_attempts_lock_the_account_even_for_the_right_password()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await _client.LoginAsync(email, "Wr0ngPassword");
        }

        HttpResponseMessage response = await _client.LoginAsync(email, ApiClientExtensions.ValidPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Too many", (await response.ReadJsonAsync()).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Successful_login_resets_the_failed_attempt_count()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);

        for (int attempt = 0; attempt < 4; attempt++)
        {
            await _client.LoginAsync(email, "Wr0ngPassword");
        }

        await _client.LoginAndGetTokenAsync(email, ApiClientExtensions.ValidPassword);
        await _client.LoginAsync(email, "Wr0ngPassword");

        HttpResponseMessage response = await _client.LoginAsync(email, ApiClientExtensions.ValidPassword);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_account_returns_403_only_with_the_right_password()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);
        await SetStatusAsync(email, AccountStatus.Disabled);

        HttpResponseMessage rightPassword = await _client.LoginAsync(email, ApiClientExtensions.ValidPassword);
        HttpResponseMessage wrongPassword = await _client.LoginAsync(email, "Wr0ngPassword");

        Assert.Equal(HttpStatusCode.Forbidden, rightPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
    }

    [Fact]
    public async Task Deleted_account_cannot_log_in()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAndGetTokenAsync(email);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.Where(u => u.Email == email)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletedAt, DateTimeOffset.UtcNow));
        }

        HttpResponseMessage response = await _client.LoginAsync(email, ApiClientExtensions.ValidPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task SetStatusAsync(string email, AccountStatus status)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Users.Where(u => u.Email == email).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status));
    }
}
