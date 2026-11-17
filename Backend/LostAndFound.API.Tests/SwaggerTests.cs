using System.Net;
using System.Text.Json;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests;

public sealed class SwaggerTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    [Fact]
    public async Task Swagger_document_is_generated_with_the_bearer_scheme()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement document = await response.ReadJsonAsync();
        JsonElement scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/auth/reset-password", out _));
    }
}
