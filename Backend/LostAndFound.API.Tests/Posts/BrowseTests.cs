using System.Net;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Posts;

public sealed class BrowseTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Browse_is_public_and_shows_only_active_visible_posts()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        PostResponse active = await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker} active"));
        PostResponse resolved = await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker} resolved"));
        PostResponse deleted = await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker} deleted"));
        PostResponse hidden = await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker} hidden"));
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{resolved.Id}/resolve", token);
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{deleted.Id}", token);
        await factory.WithDbAsync(db => db.Posts.Where(p => p.Id == hidden.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, PostVisibility.Hidden)));

        PagedResponse<PostSummaryResponse> page = await _client.BrowseAsync($"search={marker}");

        Assert.Equal(active.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Filters_combine_type_category_location_and_date()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        PostResponse match = await _client.CreatePostAsync(token, PostApi.Body(
            type: "Lost", itemName: $"{marker} a", category: "Keys", campusLocationId: PostApi.MainGateLocationId, dateOccurred: "2026-10-05"));
        await _client.CreatePostAsync(token, PostApi.Body(
            type: "Found", itemName: $"{marker} b", category: "Keys", campusLocationId: PostApi.MainGateLocationId, dateOccurred: "2026-10-05"));
        await _client.CreatePostAsync(token, PostApi.Body(
            type: "Lost", itemName: $"{marker} c", category: "Books", campusLocationId: PostApi.MainGateLocationId, dateOccurred: "2026-10-05"));
        await _client.CreatePostAsync(token, PostApi.Body(
            type: "Lost", itemName: $"{marker} d", category: "Keys", campusLocationId: PostApi.LibraryLocationId, dateOccurred: "2026-10-05"));
        await _client.CreatePostAsync(token, PostApi.Body(
            type: "Lost", itemName: $"{marker} e", category: "Keys", campusLocationId: PostApi.MainGateLocationId, dateOccurred: "2026-10-06"));

        PagedResponse<PostSummaryResponse> page = await _client.BrowseAsync(
            $"search={marker}&type=Lost&category=Keys&campusLocationId={PostApi.MainGateLocationId}&date=2026-10-05");

        Assert.Equal(match.Id, Assert.Single(page.Items).Id);
    }

    [Theory]
    [InlineData("itemName")]
    [InlineData("description")]
    [InlineData("brand")]
    [InlineData("color")]
    public async Task Search_matches_each_text_field_ignoring_case(string field)
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        var body = PostApi.Body();
        body[field] = field == "description" ? $"Found near the gate, label says {marker}" : $"{marker}X";
        PostResponse post = await _client.CreatePostAsync(token, body);

        PagedResponse<PostSummaryResponse> page = await _client.BrowseAsync($"search={marker.ToUpperInvariant()}");

        Assert.Equal(post.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task Search_works_with_arabic_text()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body(
            itemName: $"جزدان أسود {marker}", description: "جزدان جلد أسود فيه هوية جامعية"));

        PagedResponse<PostSummaryResponse> page = await _client.BrowseAsync($"search={Uri.EscapeDataString($"جزدان أسود {marker}")}");

        Assert.Equal(post.Id, Assert.Single(page.Items).Id);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[")]
    public async Task Search_treats_wildcard_characters_literally(string wildcard)
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        PostResponse literal = await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker}{wildcard}end"));
        await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker}Zend"));

        PagedResponse<PostSummaryResponse> page = await _client.BrowseAsync(
            $"search={Uri.EscapeDataString($"{marker}{wildcard}")}");

        Assert.Equal(literal.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task Paging_and_sorting_are_consistent()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        var created = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            created.Add((await _client.CreatePostAsync(token, PostApi.Body(itemName: $"{marker} {i}"))).Id);
        }

        PagedResponse<PostSummaryResponse> newestFirst = await _client.BrowseAsync($"search={marker}&pageSize=2&page=1");
        PagedResponse<PostSummaryResponse> lastPage = await _client.BrowseAsync($"search={marker}&pageSize=2&page=3");
        PagedResponse<PostSummaryResponse> oldestFirst = await _client.BrowseAsync($"search={marker}&sort=Oldest&pageSize=5");

        Assert.Equal(5, newestFirst.TotalCount);
        Assert.Equal([created[4], created[3]], newestFirst.Items.Select(p => p.Id));
        Assert.Equal(created[0], Assert.Single(lastPage.Items).Id);
        Assert.Equal(created, oldestFirst.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task Page_past_the_end_is_empty_with_the_total()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        await _client.CreatePostAsync(token, PostApi.Body(itemName: marker));

        PagedResponse<PostSummaryResponse> page = await _client.BrowseAsync($"search={marker}&page=9");

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Theory]
    [InlineData("pageSize=51")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("type=Stolen")]
    [InlineData("category=Cars")]
    [InlineData("date=yesterday")]
    public async Task Invalid_query_values_return_400(string query)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/posts?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Card_shows_a_short_description_and_the_first_image_as_thumbnail()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        string longDescription = new('a', 400);
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body(itemName: marker, description: longDescription));
        await factory.WithDbAsync(async db =>
        {
            db.PostImages.AddRange(
                new PostImage { PostId = post.Id, StorageKey = "second.jpg", SortOrder = 1, CreatedAt = DateTimeOffset.UtcNow },
                new PostImage { PostId = post.Id, StorageKey = "first.jpg", SortOrder = 0, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        });

        PostSummaryResponse card = Assert.Single((await _client.BrowseAsync($"search={marker}")).Items);
        PostResponse details = await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}");

        Assert.Equal(new string('a', 150) + "…", card.ShortDescription);
        Assert.Equal("/uploads/first.jpg", card.ThumbnailUrl);
        Assert.Equal(["/uploads/first.jpg", "/uploads/second.jpg"], details.Images.Select(i => i.Url));
    }

    [Fact]
    public async Task Card_without_images_has_no_thumbnail_and_keeps_short_descriptions_whole()
    {
        string marker = PostApi.Marker();
        string token = await NewUserAsync();
        await _client.CreatePostAsync(token, PostApi.Body(itemName: marker, description: "Short description"));

        PostSummaryResponse card = Assert.Single((await _client.BrowseAsync($"search={marker}")).Items);

        Assert.Null(card.ThumbnailUrl);
        Assert.Equal("Short description", card.ShortDescription);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
}
