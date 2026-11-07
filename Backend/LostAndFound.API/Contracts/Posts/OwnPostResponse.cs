namespace LostAndFound.Api.Contracts.Posts;

/// <summary>A post as seen by its owner, including the private verification details for the edit form.</summary>
public sealed record OwnPostResponse(PostResponse Post, string? VerificationDetails);
