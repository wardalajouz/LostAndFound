using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Contracts.Admin;

/// <summary>Includes verification details so security staff can check ownership of handed-over items.</summary>
public sealed record AdminPostDetailsResponse(
    PostResponse Post,
    PostVisibility Visibility,
    string? VerificationDetails,
    DateTimeOffset? ResolvedAt,
    IReadOnlyList<ModerationLogEntryResponse> ModerationHistory);
