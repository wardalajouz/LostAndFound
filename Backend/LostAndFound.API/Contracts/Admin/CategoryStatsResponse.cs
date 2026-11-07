using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record CategoryStatsResponse(ItemCategory Category, int LostCount, int FoundCount);
