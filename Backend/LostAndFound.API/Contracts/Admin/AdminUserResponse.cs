using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    UserType UserType,
    UserRole Role,
    AccountStatus Status,
    int PostCount,
    DateTimeOffset CreatedAt);
