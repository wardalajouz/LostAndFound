using System.Net;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Security;

public sealed class CorsTests(CorsTests.TwoOriginsFactory factory) : IClassFixture<CorsTests.TwoOriginsFactory>
{
    private const string Frontend = "http://localhost:5173";
    private const string LiveFrontend = "https://findly.example";

    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(Frontend)]
    [InlineData(LiveFrontend)]
    public async Task Allowed_frontend_passes_the_preflight_with_the_login_header(string origin)
    {
        HttpResponseMessage response = await PreflightAsync(origin, "POST", "authorization,content-type");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(origin, Single(response, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", Single(response, "Access-Control-Allow-Methods"));
        Assert.Contains("authorization", Single(response, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("http://localhost:5174")]
    [InlineData("https://findly.example.evil.test")]
    [InlineData("null")]
    public async Task Other_sites_get_no_permission(string origin)
    {
        HttpResponseMessage preflight = await PreflightAsync(origin, "POST", "authorization");
        HttpResponseMessage get = await GetWithOriginAsync(origin);

        Assert.False(preflight.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(get.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Normal_requests_from_the_frontend_are_allowed_and_can_read_the_location_header()
    {
        HttpResponseMessage response = await GetWithOriginAsync(Frontend);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Frontend, Single(response, "Access-Control-Allow-Origin"));
        Assert.Contains("Location", Single(response, "Access-Control-Expose-Headers"));
    }

    [Fact]
    public async Task Methods_the_api_does_not_use_are_not_allowed()
    {
        HttpResponseMessage response = await PreflightAsync(Frontend, "PATCH", "content-type");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Methods")
            && Single(response, "Access-Control-Allow-Methods").Contains("PATCH", StringComparison.Ordinal));
    }

    private Task<HttpResponseMessage> PreflightAsync(string origin, string method, string headers)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", headers);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> GetWithOriginAsync(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/campus-locations");
        request.Headers.Add("Origin", origin);
        return _client.SendAsync(request);
    }

    private static string Single(HttpResponseMessage response, string header) =>
        string.Join(",", response.Headers.GetValues(header));

    public sealed class TwoOriginsFactory : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings)
            {
                ["Cors:AllowedOrigins:0"] = Frontend,
                ["Cors:AllowedOrigins:1"] = LiveFrontend
            };
    }
}

/// <summary>A wrong CORS setting must stop the API from starting, not silently allow or block everyone.</summary>
public sealed class CorsConfigurationTests
{
    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData("https://findly.example/")]
    [InlineData("https://findly.example/app")]
    [InlineData("findly.example")]
    [InlineData("ftp://findly.example")]
    [InlineData("https://user@findly.example")]
    public void Invalid_origin_stops_startup(string origin)
    {
        using var factory = new OriginFactory(origin);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("AllowedOrigins", exception.Message);
    }

    private sealed class OriginFactory(string origin) : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings) { ["Cors:AllowedOrigins:0"] = origin };
    }
}
