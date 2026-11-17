namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>Same as <see cref="FindlyApiFactory"/>, with the admin account seeded at startup.</summary>
public sealed class AdminApiFactory : FindlyApiFactory
{
    public const string AdminEmail = "security@findly.test";
    public const string AdminPassword = "Adm1nPassword";

    protected override IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string>(base.Settings)
        {
            ["AdminSeed:Email"] = AdminEmail,
            ["AdminSeed:Password"] = AdminPassword
        };
}
