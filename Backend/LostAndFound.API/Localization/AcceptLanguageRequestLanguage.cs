using LostAndFound.Api.Contracts.Profile;
using Microsoft.Net.Http.Headers;

namespace LostAndFound.Api.Localization;

/// <summary>
/// Reads the Accept-Language header sent by the frontend's language toggle.
/// Arabic is the default, matching the default for new accounts.
/// </summary>
public sealed class AcceptLanguageRequestLanguage(IHttpContextAccessor httpContextAccessor) : IRequestLanguage
{
    public PreferredLanguage Current
    {
        get
        {
            IList<StringWithQualityHeaderValue>? languages =
                httpContextAccessor.HttpContext?.Request.GetTypedHeaders().AcceptLanguage;

            string? preferred = languages?
                .OrderByDescending(language => language.Quality ?? 1)
                .Select(language => language.Value.Value)
                .FirstOrDefault(value => !string.IsNullOrEmpty(value) && value != "*");

            return preferred is not null && preferred.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                ? PreferredLanguage.English
                : PreferredLanguage.Arabic;
        }
    }
}
