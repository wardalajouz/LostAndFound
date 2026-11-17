using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Auth;

/// <summary>Bound from the "Frontend" section.</summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>Frontend page that reads "email" and "token" from the query string, for example https://findly.example/reset-password.</summary>
    [Required, Url]
    public string ResetPasswordUrl { get; init; } = string.Empty;
}
