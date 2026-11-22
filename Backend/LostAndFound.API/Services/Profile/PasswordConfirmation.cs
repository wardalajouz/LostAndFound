using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Api.Services.Profile;

/// <summary>
/// Checks the password a signed-in user types to confirm a sensitive change.
/// Wrong guesses count toward the same lockout as login, so this cannot be used to guess passwords.
/// A wrong password is a 400 field error, not a 401, because the user is still signed in.
/// </summary>
public sealed class PasswordConfirmation(UserManager<User> userManager)
{
    public async Task ConfirmAsync(User user, string? password, string field)
    {
        if (await userManager.IsLockedOutAsync(user))
        {
            throw new RequestValidationException(field, "Too many wrong passwords. Try again in a few minutes.");
        }

        if (string.IsNullOrEmpty(password))
        {
            throw new RequestValidationException(field, "Enter your current password to confirm this change.");
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            throw new RequestValidationException(field, "The password is incorrect.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
    }
}
