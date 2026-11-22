using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Auth;

/// <summary>The API must refuse to start with unsafe or missing configuration rather than run with it.</summary>
public sealed class StartupConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void Missing_or_short_signing_key_stops_startup(string signingKey)
    {
        using var factory = new SigningKeyFactory(signingKey);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("SigningKey", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Missing_or_invalid_support_inbox_stops_startup(string inbox)
    {
        using var factory = new SupportInboxFactory(inbox);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("InboxEmail", exception.Message);
    }

    private sealed class SupportInboxFactory(string inbox) : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings) { ["Support:InboxEmail"] = inbox };
    }

    private sealed class SigningKeyFactory(string signingKey) : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings) { ["Jwt:SigningKey"] = signingKey };
    }
}
