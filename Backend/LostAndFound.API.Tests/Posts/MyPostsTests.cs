using System.Net;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Posts;

public sealed class MyPostsTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Lists_only_my_posts_including_resolved_and_hidden_but_not_deleted()
    {
        string me = await NewUserAsync();
        string someoneElse = await NewUserAsync();
        PostResponse active = await _client.CreatePostAsync(me, PostApi.Body());
        PostResponse resolved = await _client.CreatePostAsync(me, PostApi.Body());
        PostResponse hidden = await _client.CreatePostAsync(me, PostApi.Body());
        PostResponse deleted = await _client.CreatePostAsync(me, PostApi.Body());
        await _client.CreatePostAsync(someoneElse, PostApi.Body());
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{resolved.Id}/resolve", me);
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{deleted.Id}", me);
        await factory.WithDbAsync(db => db.Posts.Where(p => p.Id == hidden.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, PostVisibility.Hidden)));

        var page = await _client.GetAsync<PagedResponse<PostSummaryResponse>>("/api/users/me/posts", me);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(
            new[] { active.Id, resolved.Id, hidden.Id }.OrderBy(id => id),
            page.Items.Select(p => p.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task Filters_by_type_status_and_search()
    {
        string me = await NewUserAsync();
        PostResponse lostActive = await _client.CreatePostAsync(me, PostApi.Body(type: "Lost", itemName: "Blue umbrella"));
        PostResponse lostResolved = await _client.CreatePostAsync(me, PostApi.Body(type: "Lost", itemName: "Red umbrella"));
        PostResponse found = await _client.CreatePostAsync(me, PostApi.Body(type: "Found", itemName: "Keys"));
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{lostResolved.Id}/resolve", me);

        var lost = await _client.GetAsync<PagedResponse<PostSummaryResponse>>("/api/users/me/posts?type=Lost", me);
        var resolved = await _client.GetAsync<PagedResponse<PostSummaryResponse>>("/api/users/me/posts?status=Resolved", me);
        var search = await _client.GetAsync<PagedResponse<PostSummaryResponse>>("/api/users/me/posts?search=blue", me);
        var foundOnly = await _client.GetAsync<PagedResponse<PostSummaryResponse>>("/api/users/me/posts?type=Found", me);

        Assert.Equal(2, lost.TotalCount);
        Assert.Equal(lostResolved.Id, Assert.Single(resolved.Items).Id);
        Assert.Equal(lostActive.Id, Assert.Single(search.Items).Id);
        Assert.Equal(found.Id, Assert.Single(foundOnly.Items).Id);
    }

    [Fact]
    public async Task Own_post_details_include_the_verification_details()
    {
        string me = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(me, PostApi.Body(verificationDetails: "  Wallpaper is a cat  "));

        var own = await _client.GetAsync<OwnPostResponse>($"/api/users/me/posts/{post.Id}", me);

        Assert.Equal(post.Id, own.Post.Id);
        Assert.Equal("Wallpaper is a cat", own.VerificationDetails);
    }

    [Fact]
    public async Task Someone_elses_post_returns_404_and_reveals_nothing()
    {
        string owner = await NewUserAsync();
        string stranger = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(owner, PostApi.Body(verificationDetails: "SECRET-lining"));

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Get, $"/api/users/me/posts/{post.Id}", stranger);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("SECRET-lining", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Stats_count_my_posts_by_type_and_status()
    {
        string me = await NewUserAsync();
        PostResponse lost = await _client.CreatePostAsync(me, PostApi.Body(type: "Lost"));
        await _client.CreatePostAsync(me, PostApi.Body(type: "Found"));
        await _client.CreatePostAsync(me, PostApi.Body(type: "Found"));
        PostResponse deleted = await _client.CreatePostAsync(me, PostApi.Body(type: "Found"));
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{lost.Id}/resolve", me);
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{deleted.Id}", me);
        await _client.CreatePostAsync(await NewUserAsync(), PostApi.Body(type: "Lost"));

        var stats = await _client.GetAsync<PostStatsResponse>("/api/users/me/posts/stats", me);

        Assert.Equal(new PostStatsResponse(LostCount: 1, FoundCount: 2, ActiveCount: 2, ResolvedCount: 1), stats);
    }

    [Fact]
    public async Task Stats_are_zero_for_a_new_account()
    {
        var stats = await _client.GetAsync<PostStatsResponse>("/api/users/me/posts/stats", await NewUserAsync());

        Assert.Equal(new PostStatsResponse(0, 0, 0, 0), stats);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
}
