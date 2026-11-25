using System.Net;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Security;

/// <summary>In production, nothing internal may leak to callers, and developer tools are off.</summary>
public sealed class ProductionModeTests(ProductionModeTests.ProductionFactory factory) : IClassFixture<ProductionModeTests.ProductionFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Server_errors_reveal_no_exception_details()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, "/api/posts", token, PostApi.Body());
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("Simulated database failure", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain(" at ", body);
        Assert.DoesNotContain("LostAndFound.Api", body);
    }

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/index.html")]
    public async Task Swagger_is_not_available(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_api_itself_works()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/campus-locations")).StatusCode);
    }

    public sealed class ProductionFactory : FindlyApiFactory
    {
        protected override string EnvironmentName => "Production";

        // Production refuses to start without SMTP; the tests replace the email service anyway.
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings) { ["Email:Smtp:Host"] = "smtp.invalid" };
    }
}
