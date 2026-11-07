namespace LostAndFound.Api.Contracts.Users;

public sealed record UserSummaryResponse(Guid Id, string FullName, string? AvatarUrl);
