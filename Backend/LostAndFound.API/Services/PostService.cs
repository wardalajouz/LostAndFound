using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public sealed class PostService : IPostService
{
    public Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PostResponse> UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
