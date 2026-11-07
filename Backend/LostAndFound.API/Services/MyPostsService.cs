using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

public sealed class MyPostsService : IMyPostsService
{
    public Task<PagedResponse<PostSummaryResponse>> GetAsync(MyPostsQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<OwnPostResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PostStatsResponse> GetStatsAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
