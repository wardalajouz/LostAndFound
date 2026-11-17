using Microsoft.IdentityModel.JsonWebTokens;

namespace LostAndFound.Api.Auth;

/// <summary>Claim names used in issued tokens. Incoming claims are not remapped, so these are read as-is.</summary>
public static class AuthClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;

    public const string Name = JwtRegisteredClaimNames.Name;

    public const string Role = "role";
}
