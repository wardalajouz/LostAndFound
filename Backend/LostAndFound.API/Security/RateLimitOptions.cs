using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Security;

/// <summary>Bound from the "RateLimiting" section. Each limit allows <c>PermitLimit</c> requests per window.</summary>
public sealed class RateLimitOptions : IValidatableObject
{
    public const string SectionName = "RateLimiting";

    /// <summary>Register, login, forgot and reset password, per client IP. Stops password guessing across many accounts.</summary>
    [Required]
    public FixedWindowLimit Auth { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Support tickets, per client IP. Each one sends an email, so this stops inbox flooding.</summary>
    [Required]
    public FixedWindowLimit Support { get; init; } = new() { PermitLimit = 5, WindowSeconds = 600 };

    /// <summary>Sending chat messages, per signed-in user. Stops message spam.</summary>
    [Required]
    public FixedWindowLimit Messages { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        NestedOptionsValidation.Validate(Auth, $"{SectionName}:{nameof(Auth)}")
            .Concat(NestedOptionsValidation.Validate(Support, $"{SectionName}:{nameof(Support)}"))
            .Concat(NestedOptionsValidation.Validate(Messages, $"{SectionName}:{nameof(Messages)}"));
}

public sealed class FixedWindowLimit
{
    [Range(1, 1_000_000)]
    public int PermitLimit { get; init; }

    [Range(1, 86_400)]
    public int WindowSeconds { get; init; }
}

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Support = "support";
    public const string Messages = "messages";
}
