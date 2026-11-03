namespace LostAndFound.Api.Contracts.Posts;

public sealed record PostResponse(
    Guid Id,
    PostType Type,
    ItemCategory Category,
    string Description,
    string? Color,
    LocationDto Location,
    DateOnly DateOccurred,
    PostStatus Status,
    DateTimeOffset CreatedAt);
