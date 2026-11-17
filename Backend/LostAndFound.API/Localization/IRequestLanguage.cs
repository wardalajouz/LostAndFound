using LostAndFound.Api.Contracts.Profile;

namespace LostAndFound.Api.Localization;

/// <summary>The language the response should use, for example for campus location names.</summary>
public interface IRequestLanguage
{
    PreferredLanguage Current { get; }
}
