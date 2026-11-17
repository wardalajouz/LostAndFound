using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LostAndFound.Api.Contracts.Auth;

namespace LostAndFound.Api.Tests.Infrastructure;

public static class ApiClientExtensions
{
    public const string ValidPassword = "Str0ngPassword";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@findly.test";

    public static object RegisterBody(string email, string password = ValidPassword) => new
    {
        fullName = "Ward Alajouz",
        email,
        userType = "Student",
        password,
        confirmPassword = password,
        acceptTerms = true
    };

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = ValidPassword) =>
        client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, password));

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    public static async Task<string> RegisterAndGetTokenAsync(this HttpClient client, string email)
    {
        HttpResponseMessage response = await client.RegisterAsync(email);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!.AccessToken;
    }

    public static async Task<string> LoginAndGetTokenAsync(this HttpClient client, string email, string password)
    {
        HttpResponseMessage response = await client.LoginAsync(email, password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!.AccessToken;
    }

    public static Task<HttpResponseMessage> SendWithTokenAsync(this HttpClient client, HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client.SendAsync(request);
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);
}
