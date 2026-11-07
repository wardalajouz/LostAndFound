namespace LostAndFound.Api.Contracts.Posts;

public sealed record PostStatsResponse(int LostCount, int FoundCount, int ActiveCount, int ResolvedCount);
