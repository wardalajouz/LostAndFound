using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Services;

/// <summary>Posts owned by the signed-in user.</summary>
public interface IMyPostsService
{
    Task<PagedResponse<PostSummaryResponse>> GetAsync(MyPostsQuery query, CancellationToken cancellationToken);

    Task<OwnPostResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PostStatsResponse> GetStatsAsync(CancellationToken cancellationToken);
}
