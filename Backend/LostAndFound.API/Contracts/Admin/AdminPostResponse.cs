using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record AdminPostResponse(
    Guid Id,
    PostType Type,
    string ItemName,
    ItemCategory Category,
    PostStatus Status,
    PostVisibility Visibility,
    bool HandedToSecurity,
    CampusLocationResponse CampusLocation,
    DateOnly DateOccurred,
    UserSummaryResponse Publisher,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
