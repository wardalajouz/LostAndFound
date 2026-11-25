using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Conversations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Security;

/// <summary>Each test gets its own API with small limits, because rate limit counters are kept in memory.</summary>
public sealed class RateLimitTests : IAsyncLifetime
{
    private const int AuthLimit = 3;
    private const int SupportLimit = 2;
    private const int MessagesLimit = 4;

    private readonly LowLimitsFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Login_attempts_beyond_the_limit_get_429_with_retry_after()
    {
        for (int i = 0; i < AuthLimit; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync("nobody@findly.test", "Wr0ngPassword")).StatusCode);
        }

        HttpResponseMessage blocked = await _client.LoginAsync("nobody@findly.test", "Wr0ngPassword");

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType!.MediaType);
        Assert.Equal(429, (await blocked.ReadJsonAsync()).GetProperty("status").GetInt32());
        int retryAfter = int.Parse(Assert.Single(blocked.Headers.GetValues("Retry-After")));
        Assert.InRange(retryAfter, 1, 60);
        TestLogSink.LogEntry log = Assert.Single(_factory.Logs.WithEventId(1030));
        Assert.Contains("/api/auth/login", log.Message);
        Assert.Contains("by the auth rate limit", log.Message);
    }

    [Fact]
    public async Task All_auth_endpoints_share_one_limit()
    {
        await _client.RegisterAsync(ApiClientExtensions.UniqueEmail());
        await _client.LoginAsync("nobody@findly.test", "Wr0ngPassword");
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@findly.test" });

        HttpResponseMessage reset = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new { email = "nobody@findly.test", token = "x", newPassword = "N3wPassword", confirmNewPassword = "N3wPassword" });

        Assert.Equal(HttpStatusCode.TooManyRequests, reset.StatusCode);
    }

    [Fact]
    public async Task Blocked_login_is_not_counted_as_a_wrong_password()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _client.RegisterAsync(email);
        for (int i = 0; i < 10; i++)
        {
            await _client.LoginAsync(email, "Wr0ngPassword");
        }

        // Only the requests that got through (the limit minus the register call) reached the password check.
        await _factory.WithDbAsync(async db =>
        {
            var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.Users, u => u.Email == email);
            Assert.Equal(AuthLimit - 1, user.AccessFailedCount);
        });
    }

    [Fact]
    public async Task Endpoints_without_a_limit_are_not_affected()
    {
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/campus-locations")).StatusCode);
        }
    }

    [Fact]
    public async Task Support_form_is_limited()
    {
        var ticket = new { fullName = "Ward", email = "ward@findly.test", subject = "Help me", message = "Long enough message." };
        for (int i = 0; i < SupportLimit; i++)
        {
            Assert.Equal(HttpStatusCode.Accepted, (await _client.PostAsJsonAsync("/api/support-tickets", ticket)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await _client.PostAsJsonAsync("/api/support-tickets", ticket)).StatusCode);
    }

    [Fact]
    public async Task Message_limit_is_per_user()
    {
        // Registering three users would use up the small auth limit, so accounts are made directly.
        (string shatha, PostResponse post) = await NewUserWithPostAsync();
        string ward = await NewUserAsync();
        string ahmad = await NewUserAsync();
        ConversationResponse wardChat = await _client.StartChatOkAsync(ward, post.Id);
        ConversationResponse ahmadChat = await _client.StartChatOkAsync(ahmad, post.Id);

        for (int i = 0; i < MessagesLimit; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await _client.SendMessageAsync(ward, wardChat.Id, $"message {i}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await _client.SendMessageAsync(ward, wardChat.Id, "one too many")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendMessageAsync(ahmad, ahmadChat.Id, "my own limit")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendMessageAsync(shatha, wardChat.Id, "reply")).StatusCode);
    }

    [Theory]
    [InlineData("RateLimiting:Auth:PermitLimit", "0")]
    [InlineData("RateLimiting:Messages:WindowSeconds", "0")]
    [InlineData("RateLimiting:Support:PermitLimit", "-5")]
    public void Invalid_limits_stop_startup(string key, string value)
    {
        using var factory = new SettingFactory(key, value);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains(key, exception.Message);
    }

    private async Task<string> NewUserAsync()
    {
        string email = ApiClientExtensions.UniqueEmail();
        await _factory.CreateUserAsync(email);
        return await _factory.TokenForAsync(email);
    }

    private async Task<(string Token, PostResponse Post)> NewUserWithPostAsync()
    {
        string token = await NewUserAsync();
        return (token, await _client.CreatePostAsync(token, PostApi.Body()));
    }

    private sealed class LowLimitsFactory : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings)
            {
                ["RateLimiting:Auth:PermitLimit"] = AuthLimit.ToString(),
                ["RateLimiting:Support:PermitLimit"] = SupportLimit.ToString(),
                ["RateLimiting:Messages:PermitLimit"] = MessagesLimit.ToString()
            };
    }

    private sealed class SettingFactory(string key, string value) : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings) { [key] = value };
    }
}
