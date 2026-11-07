using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public sealed class PostImageService : IPostImageService
{
    public Task<IReadOnlyList<PostImageResponse>> UploadAsync(
        Guid postId, UploadPostImagesRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(Guid postId, Guid imageId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
