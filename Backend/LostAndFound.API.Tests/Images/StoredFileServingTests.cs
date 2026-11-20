using System.Net;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Images;

/// <summary>Only images the API wrote can be downloaded from /uploads; nothing else on disk is reachable.</summary>
public sealed class StoredFileServingTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Images_are_public_with_safe_and_cacheable_headers()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());
        var image = Assert.Single(await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg()));

        HttpResponseMessage response = await _client.GetAsync(image.Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.True(response.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromDays(365), response.Headers.CacheControl.MaxAge);
    }

    [Theory]
    [InlineData("secret.txt")]
    [InlineData("posts/notes.html")]
    [InlineData("avatars/script.js")]
    [InlineData("posts/image.png")]
    public async Task Files_that_are_not_webp_are_never_served(string relativePath)
    {
        string path = Path.Combine(factory.StorageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "should not be downloadable");

        HttpResponseMessage response = await _client.GetAsync($"/uploads/{relativePath}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/uploads/../appsettings.json")]
    [InlineData("/uploads/..%2fappsettings.json")]
    [InlineData("/uploads/%2e%2e/%2e%2e/appsettings.json")]
    [InlineData("/uploads/posts/")]
    [InlineData("/uploads/")]
    [InlineData("/uploads/posts/00000000000000000000000000000000.webp")]
    public async Task Paths_outside_the_folder_directories_and_missing_files_are_not_found(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Jwt", await response.Content.ReadAsStringAsync());
    }
}
