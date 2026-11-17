using System.Net;
using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Posts;

/// <summary>Each test gets its own database, because some deactivate a location.</summary>
public sealed class CampusLocationTests : IAsyncLifetime
{
    private readonly FindlyApiFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Lists_all_24_locations_in_arabic_by_default_and_english_on_request()
    {
        var arabic = await _client.GetAsync<List<CampusLocationResponse>>("/api/campus-locations");
        var english = await _client.GetAsync<List<CampusLocationResponse>>("/api/campus-locations", language: "en");

        Assert.Equal(24, arabic.Count);
        Assert.Equal(Enumerable.Range(1, 24), arabic.Select(l => l.Id));
        Assert.Equal("المكتبة", arabic.Single(l => l.Id == PostApi.LibraryLocationId).Name);
        Assert.Equal("Library", english.Single(l => l.Id == PostApi.LibraryLocationId).Name);
    }

    [Fact]
    public async Task Inactive_location_is_hidden_from_the_list_and_rejected_for_new_posts()
    {
        await DeactivateAsync(PostApi.MainGateLocationId);
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

        var locations = await _client.GetAsync<List<CampusLocationResponse>>("/api/campus-locations");
        HttpResponseMessage create = await _client.SendAsync(
            HttpMethod.Post, "/api/posts", token, PostApi.Body(campusLocationId: PostApi.MainGateLocationId));

        Assert.DoesNotContain(locations, l => l.Id == PostApi.MainGateLocationId);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Existing_post_keeps_its_inactive_location_and_can_still_be_edited()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body(campusLocationId: PostApi.MainGateLocationId));
        await DeactivateAsync(PostApi.MainGateLocationId);

        HttpResponseMessage keep = await _client.SendAsync(HttpMethod.Put, $"/api/posts/{post.Id}", token,
            PostApi.UpdateBody(PostApi.Body(itemName: "Renamed", campusLocationId: PostApi.MainGateLocationId)));
        PostResponse details = await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}", language: "en");

        Assert.Equal(HttpStatusCode.OK, keep.StatusCode);
        Assert.Equal("Main Gate", details.CampusLocation.Name);
    }

    [Fact]
    public async Task Moving_a_post_to_an_inactive_location_is_rejected()
    {
        string token = await _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body(campusLocationId: PostApi.LibraryLocationId));
        await DeactivateAsync(PostApi.MainGateLocationId);

        HttpResponseMessage move = await _client.SendAsync(HttpMethod.Put, $"/api/posts/{post.Id}", token,
            PostApi.UpdateBody(PostApi.Body(campusLocationId: PostApi.MainGateLocationId)));

        Assert.Equal(HttpStatusCode.BadRequest, move.StatusCode);
    }

    private Task DeactivateAsync(int locationId) =>
        _factory.WithDbAsync(db => db.CampusLocations.Where(l => l.Id == locationId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsActive, false)));
}
