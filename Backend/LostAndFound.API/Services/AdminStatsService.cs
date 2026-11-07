using LostAndFound.Api.Contracts.Admin;

namespace LostAndFound.Api.Services;

public sealed class AdminStatsService : IAdminStatsService
{
    public Task<AdminStatsResponse> GetAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
