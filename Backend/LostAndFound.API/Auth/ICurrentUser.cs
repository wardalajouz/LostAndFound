namespace LostAndFound.Api.Auth;

/// <summary>The signed-in user, taken from the verified access token. Never from the request body.</summary>
public interface ICurrentUser
{
    /// <summary>Throws when the request has no signed-in user.</summary>
    Guid Id { get; }
}
