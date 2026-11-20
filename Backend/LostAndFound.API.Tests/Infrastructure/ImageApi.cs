using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;

namespace LostAndFound.Api.Tests.Infrastructure;

public static class ImageApi
{
    public static MultipartFormDataContent Form(string field, params (byte[] Content, string FileName, string ContentType)[] files)
    {
        var form = new MultipartFormDataContent();
        foreach ((byte[] content, string fileName, string contentType) in files)
        {
            var part = new ByteArrayContent(content);
            part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(part, field, fileName);
        }

        return form;
    }

    public static (byte[], string, string) Jpeg(byte[]? content = null) => (content ?? TestImages.Jpeg(), "photo.jpg", "image/jpeg");

    public static Task<HttpResponseMessage> UploadPostImagesAsync(
        this HttpClient client, Guid postId, string? token, params (byte[], string, string)[] files) =>
        SendFormAsync(client, HttpMethod.Post, $"/api/posts/{postId}/images", token, Form("images", files));

    public static async Task<IReadOnlyList<PostImageResponse>> UploadPostImagesOkAsync(
        this HttpClient client, Guid postId, string token, params (byte[], string, string)[] files)
    {
        HttpResponseMessage response = await client.UploadPostImagesAsync(postId, token, files);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<PostImageResponse>>(ApiClientExtensions.Json))!;
    }

    public static Task<HttpResponseMessage> UploadAvatarAsync(this HttpClient client, string? token, (byte[], string, string) file) =>
        SendFormAsync(client, HttpMethod.Put, "/api/users/me/avatar", token, Form("image", file));

    public static async Task<AvatarResponse> UploadAvatarOkAsync(this HttpClient client, string token, (byte[], string, string) file)
    {
        HttpResponseMessage response = await client.UploadAvatarAsync(token, file);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AvatarResponse>(ApiClientExtensions.Json))!;
    }

    public static Task<HttpResponseMessage> SendFormAsync(
        HttpClient client, HttpMethod method, string url, string? token, HttpContent content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client.SendAsync(request);
    }
}
