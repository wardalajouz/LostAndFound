namespace LostAndFound.Api.Contracts.Posts;

/// <summary>A post's type cannot change. Found-only rules are checked by the service against the stored type.</summary>
public sealed record UpdatePostRequest : PostDetails;
