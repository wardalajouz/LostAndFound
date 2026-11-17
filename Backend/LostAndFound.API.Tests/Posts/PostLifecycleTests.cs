using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Posts;

public sealed class PostLifecycleTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- Create ----------

    [Fact]
    public async Task Create_returns_201_with_the_saved_post_and_its_location()
    {
        string token = await NewUserAsync();
        var body = PostApi.Body(
            itemName: "  Galaxy S23  ", brand: "   ", additionalLocationDetails: "",
            verificationDetails: "Lock screen is a cat", handedToSecurity: true);

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, "/api/posts", token, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PostResponse post = (await response.Content.ReadFromJsonAsync<PostResponse>(ApiClientExtensions.Json))!;
        Assert.Equal($"/api/posts/{post.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal("Galaxy S23", post.ItemName);
        Assert.Null(post.Brand);
        Assert.Null(post.AdditionalLocationDetails);
        Assert.Equal(PostType.Found, post.Type);
        Assert.Equal(PostStatus.Active, post.Status);
        Assert.True(post.HandedToSecurity);
        Assert.Equal(PostApi.LibraryLocationId, post.CampusLocation.Id);
        Assert.Equal("المكتبة", post.CampusLocation.Name);
        Assert.Equal("Ward Alajouz", post.Publisher.FullName);
        Assert.Empty(post.Images);
    }

    [Fact]
    public async Task Create_response_never_contains_the_verification_details()
    {
        string token = await NewUserAsync();
        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Post, "/api/posts", token, PostApi.Body(verificationDetails: "SECRET-scratch-on-back"));

        string json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("SECRET-scratch-on-back", json);
        Assert.DoesNotContain("verificationDetails", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_is_saved_for_the_signed_in_user_not_a_value_from_the_body()
    {
        string token = await NewUserAsync();
        var body = PostApi.Body();
        body["ownerId"] = Guid.NewGuid();

        PostResponse post = await _client.CreatePostAsync(token, body);
        var stats = await _client.GetAsync<PostStatsResponse>("/api/users/me/posts/stats", token);

        Assert.Equal(1, stats.FoundCount);
        Assert.NotEqual(body["ownerId"], post.Publisher.Id);
    }

    [Fact]
    public async Task Create_with_an_unknown_location_returns_400()
    {
        string token = await NewUserAsync();

        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Post, "/api/posts", token, PostApi.Body(campusLocationId: 999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("CampusLocationId", out _));
    }

    [Theory]
    [InlineData("handedToSecurity", true)]
    [InlineData("verificationDetails", "inner lining is red")]
    public async Task Create_rejects_found_only_fields_on_a_lost_post(string field, object value)
    {
        string token = await NewUserAsync();
        var body = PostApi.Body(type: "Lost");
        body[field] = value;

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, "/api/posts", token, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_future_date()
    {
        string token = await NewUserAsync();
        string nextWeek = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd");

        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Post, "/api/posts", token, PostApi.Body(dateOccurred: nextWeek));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("DateOccurred", out _));
    }

    // ---------- Details ----------

    [Fact]
    public async Task Details_are_public_and_use_the_requested_language()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());

        PostResponse arabic = await _client.GetAsync<PostResponse>($"/api/posts/{created.Id}");
        PostResponse english = await _client.GetAsync<PostResponse>($"/api/posts/{created.Id}", language: "en-US");

        Assert.Equal("المكتبة", arabic.CampusLocation.Name);
        Assert.Equal("Library", english.CampusLocation.Name);
    }

    [Fact]
    public async Task Details_of_an_unknown_post_return_404()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/posts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Hidden_post_is_gone_publicly_but_still_visible_to_its_owner()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());
        await SetVisibilityAsync(created.Id, PostVisibility.Hidden);

        HttpResponseMessage publicResponse = await _client.GetAsync($"/api/posts/{created.Id}");
        HttpResponseMessage ownerResponse = await _client.SendAsync(HttpMethod.Get, $"/api/users/me/posts/{created.Id}", token);

        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Owner_can_update_and_the_type_stays_the_same()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());
        var update = PostApi.UpdateBody(PostApi.Body(
            itemName: "iPhone 15", category: "Electronics", color: "Blue", brand: "Apple",
            description: "Blue phone in a clear case", campusLocationId: PostApi.MainGateLocationId));

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Put, $"/api/posts/{created.Id}", token, update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PostResponse updated = (await response.Content.ReadFromJsonAsync<PostResponse>(ApiClientExtensions.Json))!;
        Assert.Equal("iPhone 15", updated.ItemName);
        Assert.Equal("Blue", updated.Color);
        Assert.Equal(PostApi.MainGateLocationId, updated.CampusLocation.Id);
        Assert.Equal(PostType.Found, updated.Type);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task Another_user_cannot_update_and_gets_404()
    {
        string owner = await NewUserAsync();
        string stranger = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(owner, PostApi.Body(itemName: "Original"));

        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Put, $"/api/posts/{created.Id}", stranger, PostApi.UpdateBody(PostApi.Body(itemName: "Hacked")));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Original", (await _client.GetAsync<PostResponse>($"/api/posts/{created.Id}")).ItemName);
    }

    [Fact]
    public async Task Resolved_post_cannot_be_updated()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{created.Id}/resolve", token);

        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Put, $"/api/posts/{created.Id}", token, PostApi.UpdateBody(PostApi.Body()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_rejects_found_only_fields_on_a_lost_post()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body(type: "Lost"));

        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Put, $"/api/posts/{created.Id}", token,
            PostApi.UpdateBody(PostApi.Body(verificationDetails: "private detail")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("VerificationDetails", out _));
    }

    [Fact]
    public async Task Update_with_an_unknown_location_returns_400()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());

        HttpResponseMessage response = await _client.SendAsync(
            HttpMethod.Put, $"/api/posts/{created.Id}", token, PostApi.UpdateBody(PostApi.Body(campusLocationId: 999)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_can_clear_the_verification_details()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body(verificationDetails: "scratch"));

        await _client.SendAsync(HttpMethod.Put, $"/api/posts/{created.Id}", token,
            PostApi.UpdateBody(PostApi.Body(verificationDetails: null)));
        var own = await _client.GetAsync<OwnPostResponse>($"/api/users/me/posts/{created.Id}", token);

        Assert.Null(own.VerificationDetails);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Owner_delete_hides_the_post_everywhere_but_keeps_the_row()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());

        HttpResponseMessage delete = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{created.Id}", token);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/posts/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(HttpMethod.Get, $"/api/users/me/posts/{created.Id}", token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{created.Id}", token)).StatusCode);

        await factory.WithDbAsync(async db =>
        {
            var row = await db.Posts.SingleAsync(p => p.Id == created.Id);
            Assert.Equal(PostVisibility.Deleted, row.Visibility);
        });
    }

    [Fact]
    public async Task Another_user_cannot_delete_and_gets_404()
    {
        string owner = await NewUserAsync();
        string stranger = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(owner, PostApi.Body());

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{created.Id}", stranger);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/posts/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Deleted_post_cannot_be_updated_or_resolved()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{created.Id}", token);

        HttpResponseMessage update = await _client.SendAsync(
            HttpMethod.Put, $"/api/posts/{created.Id}", token, PostApi.UpdateBody(PostApi.Body()));
        HttpResponseMessage resolve = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{created.Id}/resolve", token);

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, resolve.StatusCode);
    }

    // ---------- Resolve ----------

    [Fact]
    public async Task Owner_can_resolve_and_resolving_twice_is_harmless()
    {
        string token = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(token, PostApi.Body());

        HttpResponseMessage first = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{created.Id}/resolve", token);
        DateTimeOffset? resolvedAt = null;
        await factory.WithDbAsync(async db => resolvedAt = (await db.Posts.SingleAsync(p => p.Id == created.Id)).ResolvedAt);
        HttpResponseMessage second = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{created.Id}/resolve", token);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.NotNull(resolvedAt);
        await factory.WithDbAsync(async db =>
            Assert.Equal(resolvedAt, (await db.Posts.SingleAsync(p => p.Id == created.Id)).ResolvedAt));
        Assert.Equal(PostStatus.Resolved, (await _client.GetAsync<PostResponse>($"/api/posts/{created.Id}")).Status);
    }

    [Fact]
    public async Task Another_user_cannot_resolve_and_gets_404()
    {
        string owner = await NewUserAsync();
        string stranger = await NewUserAsync();
        PostResponse created = await _client.CreatePostAsync(owner, PostApi.Body());

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{created.Id}/resolve", stranger);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(PostStatus.Active, (await _client.GetAsync<PostResponse>($"/api/posts/{created.Id}")).Status);
    }

    // ---------- Responses ----------

    [Fact]
    public async Task Errors_use_the_problem_details_shape()
    {
        string token = await NewUserAsync();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{Guid.NewGuid()}", token);
        JsonElement body = await response.ReadJsonAsync();

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Post not found.", body.GetProperty("detail").GetString());
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

    private Task SetVisibilityAsync(Guid postId, PostVisibility visibility) =>
        factory.WithDbAsync(db => db.Posts.Where(p => p.Id == postId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, visibility)));
}
