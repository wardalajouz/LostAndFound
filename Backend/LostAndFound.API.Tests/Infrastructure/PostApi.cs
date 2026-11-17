using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>Builds post requests and calls the posts endpoints as a given user.</summary>
public static class PostApi
{
    public const int LibraryLocationId = 16;
    public const int MainGateLocationId = 17;

    public static Dictionary<string, object?> Body(
        string type = "Found",
        string itemName = "Samsung Galaxy S23",
        string category = "Electronics",
        string color = "Black",
        string? brand = "Samsung",
        string description = "Black phone with a cracked screen protector",
        int campusLocationId = LibraryLocationId,
        string? additionalLocationDetails = "Second floor",
        string dateOccurred = "2026-11-01",
        bool handedToSecurity = false,
        string? verificationDetails = null) => new()
    {
        ["type"] = type,
        ["itemName"] = itemName,
        ["category"] = category,
        ["color"] = color,
        ["brand"] = brand,
        ["description"] = description,
        ["campusLocationId"] = campusLocationId,
        ["additionalLocationDetails"] = additionalLocationDetails,
        ["dateOccurred"] = dateOccurred,
        ["handedToSecurity"] = handedToSecurity,
        ["verificationDetails"] = verificationDetails
    };

    /// <summary>The same fields without "type", which cannot be changed by an update.</summary>
    public static Dictionary<string, object?> UpdateBody(Dictionary<string, object?> body)
    {
        var copy = new Dictionary<string, object?>(body);
        copy.Remove("type");
        return copy;
    }

    public static Task<HttpResponseMessage> SendAsync(
        this HttpClient client, HttpMethod method, string url, string? token, object? body = null, string? language = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (language is not null)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: ApiClientExtensions.Json);
        }

        return client.SendAsync(request);
    }

    public static async Task<PostResponse> CreatePostAsync(this HttpClient client, string token, Dictionary<string, object?> body)
    {
        HttpResponseMessage response = await client.SendAsync(HttpMethod.Post, "/api/posts", token, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PostResponse>(ApiClientExtensions.Json))!;
    }

    public static async Task<T> GetAsync<T>(this HttpClient client, string url, string? token = null, string? language = null)
    {
        HttpResponseMessage response = await client.SendAsync(HttpMethod.Get, url, token, language: language);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ApiClientExtensions.Json))!;
    }

    public static Task<PagedResponse<PostSummaryResponse>> BrowseAsync(this HttpClient client, string query) =>
        client.GetAsync<PagedResponse<PostSummaryResponse>>($"/api/posts?{query}");

    /// <summary>A word that only appears in this test's posts, so searches ignore posts from other tests.</summary>
    public static string Marker() => $"mk{Guid.NewGuid():N}"[..14];
}
