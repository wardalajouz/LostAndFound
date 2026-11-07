using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Services;

public interface IAdminPostService
{
    Task<PagedResponse<AdminPostResponse>> GetAsync(AdminPostsQuery query, CancellationToken cancellationToken);

    Task<AdminPostDetailsResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Hides, soft-deletes, or restores a post, and logs the reason.</summary>
    Task UpdateVisibilityAsync(Guid id, UpdatePostVisibilityRequest request, CancellationToken cancellationToken);
}
