using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Services;

public interface IAdminUserService
{
    Task<PagedResponse<AdminUserResponse>> GetAsync(AdminUsersQuery query, CancellationToken cancellationToken);

    /// <summary>Disables or reactivates an account, and logs the reason.</summary>
    Task UpdateStatusAsync(Guid id, UpdateAccountStatusRequest request, CancellationToken cancellationToken);
}
