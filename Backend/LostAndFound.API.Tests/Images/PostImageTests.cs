using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Images;

public sealed class PostImageTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- Upload ----------

    [Fact]
    public async Task Owner_uploads_images_that_are_stored_served_and_listed_in_order()
    {
        (string token, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token,
            ImageApi.Jpeg(TestImages.Jpeg(color: TestImages.Red)),
            (TestImages.Png(color: TestImages.Blue), "second.png", "image/png"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var uploaded = (await response.Content.ReadFromJsonAsync<List<PostImageResponse>>(ApiClientExtensions.Json))!;
        Assert.Equal(2, uploaded.Count);
        Assert.All(uploaded, image => Assert.Matches("^/uploads/posts/[0-9a-f]{32}\\.webp$", image.Url));

        PostResponse details = await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}");
        Assert.Equal(uploaded.Select(i => i.Url), details.Images.Select(i => i.Url));

        foreach (PostImageResponse image in uploaded)
        {
            HttpResponseMessage file = await _client.GetAsync(image.Url);
            Assert.Equal(HttpStatusCode.OK, file.StatusCode);
            Assert.Equal("image/webp", file.Content.Headers.ContentType!.MediaType);
            Assert.True(TestImages.IsWebp(await file.Content.ReadAsByteArrayAsync()));
        }
    }

    [Fact]
    public async Task Later_uploads_are_added_after_existing_images_and_the_first_stays_the_thumbnail()
    {
        string marker = PostApi.Marker();
        (string token, PostResponse post) = await NewPostAsync(marker);

        var first = await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg());
        var more = await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg(), ImageApi.Jpeg());

        PostResponse details = await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}");
        PagedResponse<PostSummaryResponse> browse = await _client.BrowseAsync($"search={marker}");
        Assert.Equal(first.Concat(more).Select(i => i.Url), details.Images.Select(i => i.Url));
        Assert.Equal(first[0].Url, Assert.Single(browse.Items).ThumbnailUrl);
    }

    [Fact]
    public async Task A_post_can_have_at_most_five_images()
    {
        (string token, PostResponse post) = await NewPostAsync();
        await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg(), ImageApi.Jpeg(), ImageApi.Jpeg(), ImageApi.Jpeg());
        int filesBefore = factory.StoredFiles().Count;

        HttpResponseMessage tooMany = await _client.UploadPostImagesAsync(post.Id, token, ImageApi.Jpeg(), ImageApi.Jpeg());
        HttpResponseMessage fifth = await _client.UploadPostImagesAsync(post.Id, token, ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.Contains("at most 5", await tooMany.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Created, fifth.StatusCode);
        Assert.Equal(filesBefore + 1, factory.StoredFiles().Count);
        Assert.Equal(5, (await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images.Count);
    }

    [Fact]
    public async Task Six_files_in_one_request_are_rejected()
    {
        (string token, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token,
            ImageApi.Jpeg(), ImageApi.Jpeg(), ImageApi.Jpeg(), ImageApi.Jpeg(), ImageApi.Jpeg(), ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images);
    }

    [Fact]
    public async Task Request_without_files_is_rejected()
    {
        (string token, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await ImageApi.SendFormAsync(
            _client, HttpMethod.Post, $"/api/posts/{post.Id}/images", token, new MultipartFormDataContent { { new StringContent("x"), "other" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Json_body_is_rejected_with_415()
    {
        (string token, PostResponse post) = await NewPostAsync();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Post, $"/api/posts/{post.Id}/images", token, new { images = "x" });

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task One_bad_file_rejects_the_whole_upload_and_names_the_file()
    {
        (string token, PostResponse post) = await NewPostAsync();
        int filesBefore = factory.StoredFiles().Count;

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token,
            ImageApi.Jpeg(), ("not an image"u8.ToArray(), "evil.jpg", "image/jpeg"), ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Images[1]", out _));
        Assert.False(errors.TryGetProperty("Images[0]", out _));
        Assert.Equal(filesBefore, factory.StoredFiles().Count);
        Assert.Empty((await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images);
    }

    [Fact]
    public async Task File_type_is_decided_by_content_not_by_name_or_header()
    {
        (string token, PostResponse post) = await NewPostAsync();

        HttpResponseMessage disguised = await _client.UploadPostImagesAsync(post.Id, token,
            ("<html>hi</html>"u8.ToArray(), "photo.png", "image/png"));
        HttpResponseMessage misnamed = await _client.UploadPostImagesAsync(post.Id, token,
            (TestImages.Png(), "notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, disguised.StatusCode);
        Assert.Equal(HttpStatusCode.Created, misnamed.StatusCode);
    }

    [Fact]
    public async Task File_over_5_mb_is_rejected()
    {
        (string token, PostResponse post) = await NewPostAsync();
        byte[] big = [.. TestImages.Png(), .. new byte[6 * 1024 * 1024]];

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token, (big, "big.png", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("5 MB", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Another_user_cannot_upload_and_nothing_is_stored()
    {
        (_, PostResponse post) = await NewPostAsync();
        string stranger = await NewUserAsync();
        int filesBefore = factory.StoredFiles().Count;

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, stranger, ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(filesBefore, factory.StoredFiles().Count);
    }

    [Fact]
    public async Task Unknown_and_deleted_posts_return_404()
    {
        (string token, PostResponse post) = await NewPostAsync();
        await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}", token);

        HttpResponseMessage deleted = await _client.UploadPostImagesAsync(post.Id, token, ImageApi.Jpeg());
        HttpResponseMessage unknown = await _client.UploadPostImagesAsync(Guid.NewGuid(), token, ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Resolved_post_images_cannot_be_added_or_removed()
    {
        (string token, PostResponse post) = await NewPostAsync();
        var image = Assert.Single(await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg()));
        await _client.SendAsync(HttpMethod.Post, $"/api/posts/{post.Id}/resolve", token);

        HttpResponseMessage add = await _client.UploadPostImagesAsync(post.Id, token, ImageApi.Jpeg());
        HttpResponseMessage remove = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}/images/{image.Id}", token);

        Assert.Equal(HttpStatusCode.Conflict, add.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, remove.StatusCode);
    }

    [Fact]
    public async Task Owner_can_still_manage_images_of_a_post_hidden_by_an_admin()
    {
        (string token, PostResponse post) = await NewPostAsync();
        await factory.WithDbAsync(db => db.Posts.Where(p => p.Id == post.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, PostVisibility.Hidden)));

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token, ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Database_failure_removes_the_files_already_written()
    {
        (string token, PostResponse post) = await NewPostAsync();
        int filesBefore = factory.StoredFiles().Count;
        factory.SaveFailures.FailNextSave();

        HttpResponseMessage response = await _client.UploadPostImagesAsync(post.Id, token, ImageApi.Jpeg(), ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(filesBefore, factory.StoredFiles().Count);
        Assert.Empty((await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Owner_deletes_an_image_and_its_file()
    {
        (string token, PostResponse post) = await NewPostAsync();
        var images = await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg(), ImageApi.Jpeg());

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}/images/{images[0].Id}", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(images[0].Url)).StatusCode);
        Assert.DoesNotContain(images[0].Url.Replace("/uploads/", ""), factory.StoredFiles());
        Assert.Equal([images[1].Url], (await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images.Select(i => i.Url));
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}/images/{images[0].Id}", token)).StatusCode);
    }

    [Fact]
    public async Task Another_user_cannot_delete_an_image()
    {
        (string token, PostResponse post) = await NewPostAsync();
        var image = Assert.Single(await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg()));
        string stranger = await NewUserAsync();

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}/images/{image.Id}", stranger);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(image.Url)).StatusCode);
    }

    [Fact]
    public async Task Image_must_belong_to_the_post_in_the_url()
    {
        (string token, PostResponse victimPost) = await NewPostAsync();
        var victimImage = Assert.Single(await _client.UploadPostImagesOkAsync(victimPost.Id, token, ImageApi.Jpeg()));
        (string attacker, PostResponse attackerPost) = await NewPostAsync();

        HttpResponseMessage crossPost = await _client.SendAsync(
            HttpMethod.Delete, $"/api/posts/{attackerPost.Id}/images/{victimImage.Id}", attacker);
        HttpResponseMessage ownerWrongPost = await _client.SendAsync(
            HttpMethod.Delete, $"/api/posts/{(await _client.CreatePostAsync(token, PostApi.Body())).Id}/images/{victimImage.Id}", token);

        Assert.Equal(HttpStatusCode.NotFound, crossPost.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ownerWrongPost.StatusCode);
        Assert.Single((await _client.GetAsync<PostResponse>($"/api/posts/{victimPost.Id}")).Images);
    }

    [Fact]
    public async Task Deleting_an_image_whose_file_is_already_gone_still_succeeds()
    {
        (string token, PostResponse post) = await NewPostAsync();
        var image = Assert.Single(await _client.UploadPostImagesOkAsync(post.Id, token, ImageApi.Jpeg()));
        File.Delete(Path.Combine(factory.StorageRoot, image.Url.Replace("/uploads/", "")));

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Delete, $"/api/posts/{post.Id}/images/{image.Id}", token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Images);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());

    private async Task<(string Token, PostResponse Post)> NewPostAsync(string? itemName = null)
    {
        string token = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body(itemName: itemName ?? "Black wallet"));
        return (token, post);
    }
}
