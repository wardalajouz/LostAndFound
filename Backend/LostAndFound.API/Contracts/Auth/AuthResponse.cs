namespace LostAndFound.Api.Contracts.Auth;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt);
