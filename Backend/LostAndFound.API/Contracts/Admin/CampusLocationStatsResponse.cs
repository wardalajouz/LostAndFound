using LostAndFound.Api.Contracts.CampusLocations;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record CampusLocationStatsResponse(CampusLocationResponse CampusLocation, int LostCount, int FoundCount);
