using System.Net;
using System.Text;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Images;
using LostAndFound.Api.Security;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Security;

/// <summary>
/// Request size limits are enforced by the Kestrel web server, which the in-memory test server skips.
/// These tests run the API on a real Kestrel server, bound to the loopback address only.
/// </summary>
public sealed class RequestSizeTests : IAsyncLifetime
{
    private readonly KestrelFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory.UseKestrel(port: 0);
        _factory.StartServer();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public void Server_listens_on_loopback_only()
    {
        string host = _client.BaseAddress!.Host;
        Assert.True(host == "localhost" || IPAddress.IsLoopback(IPAddress.Parse(host)), $"Bound to {host}");
    }

    [Fact]
    public async Task Json_body_over_1_mb_is_refused_with_413()
    {
        string huge = new('a', (int)SecurityServiceCollectionExtensions.MaxRequestBodyBytes + 1);
        var content = new StringContent($$"""{ "email": "a@b.test", "password": "{{huge}}" }""", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Normal_json_requests_still_work()
    {
        HttpResponseMessage response = await _client.RegisterAsync(ApiClientExtensions.UniqueEmail());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Image_uploads_keep_their_larger_limit()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());

        // Four images of about 1.5 MB each: well over the 1 MB JSON limit, under the upload limit.
        byte[] photo = [.. TestImages.Png(), .. new byte[1_500_000]];
        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token,
            (photo, "1.png", "image/png"), (photo, "2.png", "image/png"), (photo, "3.png", "image/png"), (photo, "4.png", "image/png"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Uploads_above_the_upload_limit_are_refused_and_store_nothing()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());
        byte[] tooBig = new byte[(int)ImageLimits.PostImagesRequestBytes + 1];

        // Kestrel stops reading an oversized upload and may close the connection before the client
        // finishes sending, so the client sees either a 413 or a dropped connection.
        try
        {
            HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token, (tooBig, "big.png", "image/png"));
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        }
        catch (HttpRequestException)
        {
        }

        Assert.Empty((await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images);
        Assert.Empty(_factory.StoredFiles());
    }

    private sealed class KestrelFactory : FindlyApiFactory;
}
