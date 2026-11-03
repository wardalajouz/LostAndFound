using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public interface IPostService
{
    Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken);
    Task<PostResponse> UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
