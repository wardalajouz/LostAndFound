using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Profile;

public sealed record ProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    UserType UserType,
    UserRole Role,
    string? AvatarUrl,
    DateTimeOffset CreatedAt);
