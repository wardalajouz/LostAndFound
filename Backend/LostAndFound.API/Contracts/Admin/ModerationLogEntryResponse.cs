using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record ModerationLogEntryResponse(
    PostVisibility Visibility,
    string Reason,
    UserSummaryResponse Admin,
    DateTimeOffset CreatedAt);
