namespace LostAndFound.Api.Contracts.Posts;

/// <summary>Minimal post info shown alongside a conversation.</summary>
public sealed record PostReferenceResponse(Guid Id, PostType Type, string ItemName, string? ThumbnailUrl);
