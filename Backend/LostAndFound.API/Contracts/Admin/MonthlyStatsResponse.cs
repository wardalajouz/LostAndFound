namespace LostAndFound.Api.Contracts.Admin;

public sealed record MonthlyStatsResponse(int Year, int Month, int LostCount, int FoundCount);
