using LostAndFound.Api.Contracts.CampusLocations;

namespace LostAndFound.Api.Services;

public interface ICampusLocationService
{
    Task<IReadOnlyList<CampusLocationResponse>> GetAllAsync(CancellationToken cancellationToken);
}
