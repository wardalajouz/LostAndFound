using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public interface IPostService
{
    Task<PagedResponse<PostSummaryResponse>> BrowseAsync(BrowsePostsQuery query, CancellationToken cancellationToken);

    Task<PostResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PostResponse> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken);

    Task<PostResponse> UpdateAsync(Guid id, UpdatePostRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task ResolveAsync(Guid id, CancellationToken cancellationToken);
}
