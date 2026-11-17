using LostAndFound.Api.Auth;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LostAndFound.Api.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Token_contains_the_user_roles_and_expiry()
    {
        var options = new JwtOptions { Issuer = "Findly", Audience = "Findly", SigningKey = FindlyApiFactory.SigningKey, ExpiryMinutes = 30 };
        var user = new User { Id = Guid.NewGuid(), FullName = "Shatha", Email = "shatha@findly.test" };
        var service = new JwtTokenService(Options.Create(options), new FixedTimeProvider(Now));

        var response = service.CreateAccessToken(user, [Roles.Admin]);
        var token = new JsonWebToken(response.AccessToken);

        Assert.Equal(Now.AddMinutes(30), response.ExpiresAt);
        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal("Shatha", token.GetClaim(AuthClaimTypes.Name).Value);
        Assert.Equal(Roles.Admin, token.GetClaim(AuthClaimTypes.Role).Value);
        Assert.Equal("Findly", token.Issuer);
        Assert.Contains("Findly", token.Audiences);
        Assert.Equal("HS256", token.Alg);
    }

    [Fact]
    public void Token_without_roles_has_no_role_claim()
    {
        var options = new JwtOptions { Issuer = "Findly", Audience = "Findly", SigningKey = FindlyApiFactory.SigningKey };
        var user = new User { Id = Guid.NewGuid(), FullName = "Ward", Email = "ward@findly.test" };

        var token = new JsonWebToken(new JwtTokenService(Options.Create(options), TimeProvider.System)
            .CreateAccessToken(user, []).AccessToken);

        Assert.False(token.TryGetClaim(AuthClaimTypes.Role, out _));
    }
}
