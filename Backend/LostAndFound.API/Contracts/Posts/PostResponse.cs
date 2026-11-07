using LostAndFound.Api.Contracts.CampusLocations;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Posts;

/// <summary>Public view of a post. Never contains verification details.</summary>
public sealed record PostResponse(
    Guid Id,
    PostType Type,
    string ItemName,
    ItemCategory Category,
    string Color,
    string? Brand,
    string Description,
    CampusLocationResponse CampusLocation,
    string? AdditionalLocationDetails,
    DateOnly DateOccurred,
    bool HandedToSecurity,
    PostStatus Status,
    IReadOnlyList<PostImageResponse> Images,
    UserSummaryResponse Publisher,
    DateTimeOffset CreatedAt);
