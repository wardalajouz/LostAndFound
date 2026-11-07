using LostAndFound.Api.Contracts.CampusLocations;

namespace LostAndFound.Api.Services;

public sealed class CampusLocationService : ICampusLocationService
{
    public Task<IReadOnlyList<CampusLocationResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
