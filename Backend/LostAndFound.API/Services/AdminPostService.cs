using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Services;

public sealed class AdminPostService : IAdminPostService
{
    public Task<PagedResponse<AdminPostResponse>> GetAsync(AdminPostsQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<AdminPostDetailsResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task UpdateVisibilityAsync(Guid id, UpdatePostVisibilityRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
