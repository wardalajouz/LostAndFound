using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Services;

public sealed class AdminUserService : IAdminUserService
{
    public Task<PagedResponse<AdminUserResponse>> GetAsync(AdminUsersQuery query, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task UpdateStatusAsync(Guid id, UpdateAccountStatusRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
