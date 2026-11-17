using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LostAndFound.Api.Contracts.Auth;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Auth;

public sealed class RegisterTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Valid_registration_returns_201_with_a_token_that_works()
    {
        HttpResponseMessage response = await _client.RegisterAsync(ApiClientExtensions.UniqueEmail());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        AuthResponse? body = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiClientExtensions.Json);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.True(body.ExpiresAt > DateTimeOffset.UtcNow);

        HttpResponseMessage protectedCall = await _client.SendWithTokenAsync(HttpMethod.Get, "/api/users/me/posts/stats", body.AccessToken);
        Assert.Equal(HttpStatusCode.OK, protectedCall.StatusCode);
    }

    [Fact]
    public async Task Duplicate_email_returns_409_regardless_of_letter_case()
    {
        string email = ApiClientExtensions.UniqueEmail();
        (await _client.RegisterAsync(email)).EnsureSuccessStatusCode();

        HttpResponseMessage response = await _client.RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public async Task Weak_password_returns_400_with_a_password_error(string password)
    {
        HttpResponseMessage response = await _client.RegisterAsync(ApiClientExtensions.UniqueEmail(), password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement body = await response.ReadJsonAsync();
        Assert.True(body.GetProperty("errors").TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Mismatched_confirmation_and_unaccepted_terms_return_400()
    {
        var body = new
        {
            fullName = "Ward",
            email = ApiClientExtensions.UniqueEmail(),
            userType = "Student",
            password = ApiClientExtensions.ValidPassword,
            confirmPassword = "Different1Password",
            acceptTerms = false
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("ConfirmPassword", out _));
        Assert.True(errors.TryGetProperty("AcceptTerms", out _));
    }

    [Fact]
    public async Task Unknown_user_type_returns_400()
    {
        var body = new
        {
            fullName = "Ward",
            email = ApiClientExtensions.UniqueEmail(),
            userType = "Alien",
            password = ApiClientExtensions.ValidPassword,
            confirmPassword = ApiClientExtensions.ValidPassword,
            acceptTerms = true
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
