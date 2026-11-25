using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Localization;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>Numbers and chart data for the security dashboard. Deleted posts are not counted.</summary>
public sealed class AdminStatsService(AppDbContext db, IRequestLanguage requestLanguage, TimeProvider timeProvider)
    : IAdminStatsService
{
    public const int MonthsShown = 12;

    public async Task<AdminStatsResponse> GetAsync(CancellationToken cancellationToken)
    {
        IQueryable<Post> posts = db.Posts.AsNoTracking().Where(p => p.Visibility != PostVisibility.Deleted);

        var totals = await posts
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Lost = g.Count(p => p.Type == PostType.Lost),
                Found = g.Count(p => p.Type == PostType.Found),
                Active = g.Count(p => p.Status == PostStatus.Active),
                Resolved = g.Count(p => p.Status == PostStatus.Resolved)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new AdminStatsResponse(
            totals?.Lost ?? 0,
            totals?.Found ?? 0,
            totals?.Active ?? 0,
            totals?.Resolved ?? 0,
            await ByCategoryAsync(posts, cancellationToken),
            await ByCampusLocationAsync(posts, cancellationToken),
            await ByMonthAsync(posts, cancellationToken));
    }

    /// <summary>Every category is listed, including those with no posts, so charts keep a fixed shape.</summary>
    private static async Task<IReadOnlyList<CategoryStatsResponse>> ByCategoryAsync(IQueryable<Post> posts, CancellationToken cancellationToken)
    {
        var counts = await posts
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Lost = g.Count(p => p.Type == PostType.Lost), Found = g.Count(p => p.Type == PostType.Found) })
            .ToDictionaryAsync(row => row.Category, cancellationToken);

        return Enum.GetValues<ItemCategory>()
            .Select(category => counts.TryGetValue(category, out var row)
                ? new CategoryStatsResponse(category, row.Lost, row.Found)
                : new CategoryStatsResponse(category, 0, 0))
            .ToList();
    }

    /// <summary>Only locations with posts, busiest first.</summary>
    private async Task<IReadOnlyList<CampusLocationStatsResponse>> ByCampusLocationAsync(IQueryable<Post> posts, CancellationToken cancellationToken)
    {
        bool arabic = requestLanguage.Current == PreferredLanguage.Arabic;

        var rows = await posts
            .GroupBy(p => new { p.CampusLocationId, p.CampusLocation.NameEn, p.CampusLocation.NameAr })
            .Select(g => new
            {
                g.Key.CampusLocationId,
                Name = arabic ? g.Key.NameAr : g.Key.NameEn,
                Lost = g.Count(p => p.Type == PostType.Lost),
                Found = g.Count(p => p.Type == PostType.Found)
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(row => row.Lost + row.Found)
            .ThenBy(row => row.CampusLocationId)
            .Select(row => new CampusLocationStatsResponse(new CampusLocationResponse(row.CampusLocationId, row.Name), row.Lost, row.Found))
            .ToList();
    }

    /// <summary>
    /// The last <see cref="MonthsShown"/> months in campus time, oldest first, including months with no posts.
    /// Only the two needed columns are read, and grouped here, so months follow the campus calendar.
    /// </summary>
    private async Task<IReadOnlyList<MonthlyStatsResponse>> ByMonthAsync(IQueryable<Post> posts, CancellationToken cancellationToken)
    {
        DateTimeOffset localNow = CampusTime.ToLocal(timeProvider.GetUtcNow());
        var firstMonth = new DateOnly(localNow.Year, localNow.Month, 1).AddMonths(-(MonthsShown - 1));
        DateTimeOffset windowStart = CampusTime.StartOfMonth(firstMonth.Year, firstMonth.Month);

        var created = await posts
            .Where(p => p.CreatedAt >= windowStart)
            .Select(p => new { p.CreatedAt, p.Type })
            .ToListAsync(cancellationToken);

        var byMonth = created
            .GroupBy(p => (CampusTime.ToLocal(p.CreatedAt).Year, CampusTime.ToLocal(p.CreatedAt).Month))
            .ToDictionary(g => g.Key, g => (Lost: g.Count(p => p.Type == PostType.Lost), Found: g.Count(p => p.Type == PostType.Found)));

        return Enumerable.Range(0, MonthsShown)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => byMonth.TryGetValue((month.Year, month.Month), out var count)
                ? new MonthlyStatsResponse(month.Year, month.Month, count.Lost, count.Found)
                : new MonthlyStatsResponse(month.Year, month.Month, 0, 0))
            .ToList();
    }
}
