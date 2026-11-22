using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Auth;

/// <summary>
/// Runs after a token's signature and expiry are verified, on every request that sends one.
/// A token for an account that was since deleted or disabled is rejected straight away,
/// instead of working until it expires.
/// </summary>
internal static class ActiveAccountCheck
{
    public const string FailureMessage = "This account is no longer active.";

    public static async Task OnTokenValidatedAsync(TokenValidatedContext context)
    {
        string? subject = context.Principal?.FindFirst(AuthClaimTypes.UserId)?.Value;
        if (!Guid.TryParse(subject, out Guid userId))
        {
            context.Fail("The token has no valid user id.");
            return;
        }

        AppDbContext db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        bool active = await db.Users.AnyAsync(
            u => u.Id == userId && u.DeletedAt == null && u.Status == AccountStatus.Active,
            context.HttpContext.RequestAborted);

        if (!active)
        {
            context.Fail(FailureMessage);
        }
    }
}
