using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Auth;

/// <summary>Bound from the "Jwt" section. The app refuses to start if these are invalid.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC-SHA256 key. Keep it out of source control (user secrets or an environment variable).</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; init; } = string.Empty;

    [Range(5, 1440)]
    public int ExpiryMinutes { get; init; } = 60;
}
