using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Data;
using LostAndFound.Api.Localization;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

public sealed class CampusLocationService(AppDbContext db, IRequestLanguage requestLanguage) : ICampusLocationService
{
    /// <summary>Only active locations, which are the ones new posts can use.</summary>
    public async Task<IReadOnlyList<CampusLocationResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        bool arabic = requestLanguage.Current == PreferredLanguage.Arabic;

        return await db.CampusLocations
            .AsNoTracking()
            .Where(location => location.IsActive)
            .OrderBy(location => location.Id)
            .Select(location => new CampusLocationResponse(location.Id, arabic ? location.NameAr : location.NameEn))
            .ToListAsync(cancellationToken);
    }
}
