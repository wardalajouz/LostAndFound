using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Localization;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>A signed-in user, for running services directly without an HTTP request.</summary>
public sealed class FixedCurrentUser(Guid id) : ICurrentUser
{
    public Guid Id { get; } = id;

    public Guid? OptionalId => Id;
}

public sealed class FixedRequestLanguage(PreferredLanguage language = PreferredLanguage.English) : IRequestLanguage
{
    public PreferredLanguage Current { get; } = language;
}
