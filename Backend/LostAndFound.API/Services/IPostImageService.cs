using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public interface IPostImageService
{
    Task<IReadOnlyList<PostImageResponse>> UploadAsync(
        Guid postId, UploadPostImagesRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid postId, Guid imageId, CancellationToken cancellationToken);
}
