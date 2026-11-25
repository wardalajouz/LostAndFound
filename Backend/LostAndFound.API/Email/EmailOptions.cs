using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Security;

namespace LostAndFound.Api.Email;

/// <summary>Bound from the "Email" section.</summary>
public sealed class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    [Required, EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    [Required]
    public string FromName { get; init; } = string.Empty;

    /// <summary>When Host is empty in Development, emails are written to App_Data/emails instead of being sent.</summary>
    public SmtpOptions Smtp { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        NestedOptionsValidation.Validate(Smtp, $"{SectionName}:{nameof(Smtp)}");
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public string Username { get; init; } = string.Empty;

    /// <summary>Keep this in user secrets or an environment variable, never in appsettings.json.</summary>
    public string Password { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
