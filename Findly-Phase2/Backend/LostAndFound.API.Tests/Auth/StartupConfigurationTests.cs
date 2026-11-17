using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Auth;

/// <summary>The API must refuse to start with an unsafe JWT configuration rather than run with it.</summary>
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

    private sealed class SigningKeyFactory(string signingKey) : FindlyApiFactory
    {
        protected override IReadOnlyDictionary<string, string> Settings =>
            new Dictionary<string, string>(base.Settings) { ["Jwt:SigningKey"] = signingKey };
    }
}
