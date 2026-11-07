using LostAndFound.Api.Contracts.CampusLocations;

namespace LostAndFound.Api.Contracts.Posts;

/// <summary>Item card shown in browse and "My Items" lists.</summary>
public sealed record PostSummaryResponse(
    Guid Id,
    PostType Type,
    string ItemName,
    ItemCategory Category,
    CampusLocationResponse CampusLocation,
    DateOnly DateOccurred,
    PostStatus Status,
    string ShortDescription,
    string? ThumbnailUrl);
