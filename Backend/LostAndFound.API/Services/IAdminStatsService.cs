using LostAndFound.Api.Contracts.Admin;

namespace LostAndFound.Api.Services;

public interface IAdminStatsService
{
    Task<AdminStatsResponse> GetAsync(CancellationToken cancellationToken);
}
