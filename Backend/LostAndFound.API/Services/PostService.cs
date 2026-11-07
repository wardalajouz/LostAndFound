using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public sealed class PostService : IPostService
{
    public Task<PagedResponse<PostSummaryResponse>> BrowseAsync(BrowsePostsQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PostResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PostResponse> UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task ResolveAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
