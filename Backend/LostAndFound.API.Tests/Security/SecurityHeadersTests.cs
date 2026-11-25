using System.Net;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Security;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Security;

public sealed class SecurityHeadersTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/campus-locations")]
    [InlineData("/api/users/me")]
    [InlineData("/api/posts/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/posts?pageSize=999")]
    [InlineData("/uploads/posts/missing.webp")]
    [InlineData("/does-not-exist")]
    public async Task Every_api_response_has_the_protective_headers(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url);

        AssertProtectiveHeaders(response);
        Assert.Equal(SecurityHeadersMiddleware.ContentSecurityPolicy, Header(response, "Content-Security-Policy"));
    }

    [Fact]
    public async Task Server_errors_have_the_protective_headers()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, "/api/posts", token, PostApi.Body());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertProtectiveHeaders(response);
    }

    [Fact]
    public async Task Images_have_the_protective_headers()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());
        var image = Assert.Single(await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg()));

        HttpResponseMessage response = await _client.GetAsync(image.Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertProtectiveHeaders(response);
    }

    [Fact]
    public async Task Swagger_page_still_works_and_is_left_out_of_the_content_policy()
    {
        HttpResponseMessage response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
    }

    private static void AssertProtectiveHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Header(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
    }

    private static string Header(HttpResponseMessage response, string name) =>
        Assert.Single(response.Headers.GetValues(name));
}
