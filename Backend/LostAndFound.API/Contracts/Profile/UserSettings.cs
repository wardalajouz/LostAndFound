namespace LostAndFound.Api.Contracts.Profile;

/// <summary>Used for both reading and replacing the user's settings.</summary>
public sealed record UserSettings
{
    public required PreferredLanguage Language { get; init; }

    public required bool NotifyOnNewMessage { get; init; }

    public required bool NotifyOnSimilarItem { get; init; }

    public required bool NotifyOnPostUpdate { get; init; }
}
