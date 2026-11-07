namespace LostAndFound.Api.Contracts.Admin;

public sealed record AdminStatsResponse(
    int LostCount,
    int FoundCount,
    int ActiveCount,
    int ResolvedCount,
    IReadOnlyList<CategoryStatsResponse> ByCategory,
    IReadOnlyList<CampusLocationStatsResponse> ByCampusLocation,
    IReadOnlyList<MonthlyStatsResponse> ByMonth);
