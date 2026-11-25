using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Auth;

/// <summary>Creates the Admin role and the first admin account. Safe to run on every startup.</summary>
public sealed class AdminSeeder(
    UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<AdminSeedOptions> options,
    TimeProvider timeProvider,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync()
    {
        AdminSeedOptions seed = options.Value;
        if (!seed.IsConfigured)
        {
            return;
        }

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole<Guid>(Roles.Admin)), "create the Admin role");
        }

        User? admin = await userManager.FindByEmailAsync(seed.Email!);
        if (admin is null)
        {
            admin = new User
            {
                UserName = seed.Email,
                Email = seed.Email,
                FullName = seed.FullName,
                UserType = UserType.Staff,
                CreatedAt = timeProvider.GetUtcNow()
            };
            EnsureSucceeded(await userManager.CreateAsync(admin, seed.Password!), "create the admin account");
            logger.AdminAccountCreated(admin.Id);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, Roles.Admin), "add the admin account to the Admin role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not {action}: {errors}");
        }
    }
}
