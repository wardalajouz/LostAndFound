using System.Net;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Images;

public sealed class AvatarTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Upload_stores_and_serves_the_avatar_and_shows_it_on_posts()
    {
        string token = await NewUserAsync();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());

        AvatarResponse avatar = await _client.UploadAvatarOkAsync(token, ImageApi.Jpeg());

        Assert.Matches("^/uploads/avatars/[0-9a-f]{32}\\.webp$", avatar.Url);
        HttpResponseMessage file = await _client.GetAsync(avatar.Url);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("image/webp", file.Content.Headers.ContentType!.MediaType);
        Assert.Equal(avatar.Url, (await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Publisher.AvatarUrl);
    }

    [Fact]
    public async Task Replacing_the_avatar_removes_the_old_file()
    {
        string token = await NewUserAsync();
        AvatarResponse first = await _client.UploadAvatarOkAsync(token, ImageApi.Jpeg());

        AvatarResponse second = await _client.UploadAvatarOkAsync(token, (TestImages.Png(), "new.png", "image/png"));

        Assert.NotEqual(first.Url, second.Url);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(first.Url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(second.Url)).StatusCode);
    }

    [Fact]
    public async Task Invalid_file_is_rejected_and_the_current_avatar_is_kept()
    {
        string token = await NewUserAsync();
        AvatarResponse current = await _client.UploadAvatarOkAsync(token, ImageApi.Jpeg());
        int filesBefore = factory.StoredFiles().Count;

        HttpResponseMessage response = await _client.UploadAvatarAsync(token, (TestImages.Gif1x1(), "a.gif", "image/gif"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadJsonAsync()).GetProperty("errors").TryGetProperty("Image", out _));
        Assert.Equal(filesBefore, factory.StoredFiles().Count);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(current.Url)).StatusCode);
    }

    [Fact]
    public async Task Missing_file_is_rejected()
    {
        string token = await NewUserAsync();

        HttpResponseMessage response = await ImageApi.SendFormAsync(
            _client, HttpMethod.Put, "/api/users/me/avatar", token, new MultipartFormDataContent { { new StringContent("x"), "other" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Each_user_only_changes_their_own_avatar()
    {
        string first = await NewUserAsync();
        string second = await NewUserAsync();
        PostResponse firstPost = await _client.CreatePostAsync(first, PostApi.Body());
        AvatarResponse firstAvatar = await _client.UploadAvatarOkAsync(first, ImageApi.Jpeg());

        await _client.UploadAvatarOkAsync(second, ImageApi.Jpeg());

        Assert.Equal(firstAvatar.Url, (await _client.GetAsync<PostResponse>($"/api/posts/{firstPost.Id}")).Publisher.AvatarUrl);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(firstAvatar.Url)).StatusCode);
    }

    [Fact]
    public async Task Database_failure_removes_the_new_file_and_keeps_the_old_avatar()
    {
        string token = await NewUserAsync();
        AvatarResponse current = await _client.UploadAvatarOkAsync(token, ImageApi.Jpeg());
        int filesBefore = factory.StoredFiles().Count;
        factory.SaveHooks.FailNextSave();

        HttpResponseMessage response = await _client.UploadAvatarAsync(token, ImageApi.Jpeg());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(filesBefore, factory.StoredFiles().Count);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(current.Url)).StatusCode);
    }

    private Task<string> NewUserAsync() => _client.RegisterAndGetTokenAsync(ApiClientExtensions.UniqueEmail());
}
