namespace LostAndFound.Api.Auth;

/// <summary>
/// Bound from the "AdminSeed" section. When Email and Password are set, the first admin account
/// is created at startup. Keep the password in user secrets or an environment variable.
/// </summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string? Email { get; init; }

    public string? Password { get; init; }

    public string FullName { get; init; } = "Security Admin";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
