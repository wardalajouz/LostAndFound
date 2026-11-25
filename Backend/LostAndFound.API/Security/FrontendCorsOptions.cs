using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Security;

/// <summary>
/// Bound from the "Cors" section: the browser origins allowed to call the API, for example
/// "https://findly.example". Each must be exact; a wildcard is refused, so no other site can use the API from a browser.
/// </summary>
public sealed class FrontendCorsOptions : IValidatableObject
{
    public const string SectionName = "Cors";

    [Required, MinLength(1, ErrorMessage = "Cors:AllowedOrigins needs at least one frontend address.")]
    public string[] AllowedOrigins { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (string origin in AllowedOrigins)
        {
            if (!IsExactOrigin(origin))
            {
                yield return new ValidationResult(
                    $"Cors:AllowedOrigins entry '{origin}' must be a scheme and host only, like https://findly.example.",
                    [nameof(AllowedOrigins)]);
            }
        }
    }

    /// <remarks>Wildcards such as "*" or "https://*.example" are refused because they are not valid host names.</remarks>
    private static bool IsExactOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && uri.AbsolutePath == "/"
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment)
        && string.IsNullOrEmpty(uri.UserInfo)
        && !origin.EndsWith('/');
}
