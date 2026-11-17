using System.Net;
using LostAndFound.Api.Auth;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Auth;

/// <summary>Checks who can reach each endpoint. Endpoints still return 501, so 501 means "allowed through".</summary>
public sealed class AuthorizationTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    private const string Id = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    private readonly HttpClient _client = factory.CreateClient();

    public static TheoryData<string, string> PublicEndpoints => new()
    {
        { "GET", "/api/campus-locations" },
        { "GET", "/api/posts" },
        { "GET", $"/api/posts/{Id}" }
    };

    public static TheoryData<string, string> UserEndpoints => new()
    {
        { "POST", "/api/posts" },
        { "PUT", $"/api/posts/{Id}" },
        { "DELETE", $"/api/posts/{Id}" },
        { "POST", $"/api/posts/{Id}/resolve" },
        { "POST", $"/api/posts/{Id}/images" },
        { "DELETE", $"/api/posts/{Id}/images/{Id}" },
        { "GET", "/api/users/me/posts" },
        { "GET", $"/api/users/me/posts/{Id}" },
        { "GET", "/api/users/me/posts/stats" },
        { "GET", "/api/users/me" },
        { "PUT", "/api/users/me" },
        { "PUT", "/api/users/me/avatar" },
        { "PUT", "/api/users/me/password" },
        { "GET", "/api/users/me/settings" },
        { "PUT", "/api/users/me/settings" },
        { "DELETE", "/api/users/me" },
        { "POST", "/api/conversations" },
        { "GET", "/api/conversations" },
        { "GET", $"/api/conversations/{Id}" },
        { "GET", $"/api/conversations/{Id}/messages" },
        { "POST", $"/api/conversations/{Id}/messages" },
        { "POST", $"/api/conversations/{Id}/read" },
        { "GET", "/api/notifications" },
        { "GET", "/api/notifications/unread-count" },
        { "POST", $"/api/notifications/{Id}/read" },
        { "POST", "/api/notifications/read-all" }
    };

    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/stats" },
        { "GET", "/api/admin/posts" },
        { "GET", $"/api/admin/posts/{Id}" },
        { "PUT", $"/api/admin/posts/{Id}/visibility" },
        { "GET", "/api/admin/users" },
        { "PUT", $"/api/admin/users/{Id}/status" }
    };

    [Theory]
    [MemberData(nameof(PublicEndpoints))]
    public async Task Public_endpoints_are_reachable_without_a_token(string method, string url)
    {
        HttpResponseMessage response = await _client.SendWithTokenAsync(new HttpMethod(method), url, token: null);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UserEndpoints))]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Protected_endpoints_reject_requests_without_a_token(string method, string url)
    {
        HttpResponseMessage response = await _client.SendWithTokenAsync(new HttpMethod(method), url, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
    }

    [Theory]
    [MemberData(nameof(UserEndpoints))]
    public async Task User_endpoints_let_a_signed_in_user_through(string method, string url)
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

        HttpResponseMessage response = await _client.SendWithTokenAsync(new HttpMethod(method), url, token);

        // Past authorization: either the 501 placeholder, or 400/415 because these probes send no body.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_endpoints_reject_a_normal_user_with_403(string method, string url)
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

        HttpResponseMessage response = await _client.SendWithTokenAsync(new HttpMethod(method), url, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_endpoints_let_the_seeded_admin_through(string method, string url)
    {
        string token = await _client.LoginAndGetTokenAsync(AdminApiFactory.AdminEmail, AdminApiFactory.AdminPassword);

        HttpResponseMessage response = await _client.SendWithTokenAsync(new HttpMethod(method), url, token);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Expired_token_is_rejected()
    {
        string token = CreateToken(FactoryJwtOptions(), new FixedTimeProvider(DateTimeOffset.UtcNow.AddHours(-3)));

        HttpResponseMessage response = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/users/me", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected()
    {
        JwtOptions forged = new()
        {
            Issuer = "Findly",
            Audience = "Findly",
            SigningKey = "an-attacker-key-that-is-also-long-enough-for-hmac",
            ExpiryMinutes = 60
        };

        HttpResponseMessage response = await _client.SendWithTokenAsync(
            HttpMethod.Get, "/api/users/me", CreateToken(forged, TimeProvider.System));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Tampered_token_is_rejected()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        string[] parts = token.Split('.');
        string tampered = $"{parts[0]}.{parts[1]}x.{parts[2]}";

        HttpResponseMessage response = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/users/me", tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_seeding_is_idempotent()
    {
        await using AdminApiFactory second = new();
        HttpClient client = second.CreateClient();

        string token = await client.LoginAndGetTokenAsync(AdminApiFactory.AdminEmail, AdminApiFactory.AdminPassword);
        Assert.False(string.IsNullOrEmpty(token));

        // Restarting on the same factory runs the seeder again without failing.
        HttpResponseMessage response = await _client.LoginAsync(AdminApiFactory.AdminEmail, AdminApiFactory.AdminPassword);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static JwtOptions FactoryJwtOptions() => new()
    {
        Issuer = "Findly",
        Audience = "Findly",
        SigningKey = FindlyApiFactory.SigningKey,
        ExpiryMinutes = 60
    };

    private static string CreateToken(JwtOptions options, TimeProvider time)
    {
        var user = new User { Id = Guid.NewGuid(), FullName = "Someone", Email = "someone@findly.test" };
        return new JwtTokenService(Options.Create(options), time).CreateAccessToken(user, [Roles.Admin]).AccessToken;
    }
}
