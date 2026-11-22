using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Images;
using LostAndFound.Api.Services.Posts;
using LostAndFound.Api.Services.Profile;
using LostAndFound.Api.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>The signed-in user's own account. The user always comes from the verified token.</summary>
public sealed class ProfileService(
    AppDbContext db,
    UserManager<User> userManager,
    ICurrentUser currentUser,
    PasswordConfirmation passwordConfirmation,
    AccountDeletion accountDeletion,
    IImageProcessor imageProcessor,
    IFileStorage storage,
    ILogger<ProfileService> logger) : IProfileService
{
    private const string AvatarFolder = "avatars";

    public async Task<ProfileResponse> GetAsync(CancellationToken cancellationToken) =>
        await ToResponseAsync(await LoadCurrentUserAsync(cancellationToken));

    public async Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        User user = await LoadCurrentUserAsync(cancellationToken);
        string email = request.Email.Trim();
        bool emailChanged = !string.Equals(userManager.NormalizeEmail(email), user.NormalizedEmail, StringComparison.Ordinal);

        if (emailChanged)
        {
            await passwordConfirmation.ConfirmAsync(user, request.CurrentPassword, nameof(UpdateProfileRequest.CurrentPassword));
        }

        user.FullName = request.FullName.Trim();
        user.UserType = request.UserType;
        user.UserName = email;

        // SetEmailAsync also renews the security stamp, which cancels reset links sent to the old address.
        // An unchanged address may still differ in letter case, so it is stored as typed.
        if (!emailChanged)
        {
            user.Email = email;
        }

        IdentityResult result = emailChanged
            ? await userManager.SetEmailAsync(user, email)
            : await userManager.UpdateAsync(user);

        if (IdentityErrorMapper.HasError(result, nameof(IdentityErrorDescriber.DuplicateEmail))
            || IdentityErrorMapper.HasError(result, nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        if (!result.Succeeded)
        {
            throw IdentityErrorMapper.ToValidationException(result, nameof(UpdateProfileRequest.CurrentPassword));
        }

        return await ToResponseAsync(user);
    }

    /// <summary>Replaces the profile picture. The previous file is removed once the new one is saved.</summary>
    public async Task<AvatarResponse> UploadAvatarAsync(UploadAvatarRequest request, CancellationToken cancellationToken)
    {
        User user = await LoadCurrentUserAsync(cancellationToken);

        ProcessedImage image = await imageProcessor.ProcessAsync(request.Image, nameof(UploadAvatarRequest.Image), cancellationToken);

        string newKey = await storage.SaveAsync(AvatarFolder, image.Content, image.Extension, cancellationToken);
        string? oldKey = ImageUrls.KeyFrom(user.AvatarUrl);
        try
        {
            user.AvatarUrl = ImageUrls.For(newKey);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteQuietlyAsync([newKey], logger);
            throw;
        }

        if (oldKey is not null)
        {
            await storage.DeleteQuietlyAsync([oldKey], logger);
        }

        return new AvatarResponse(user.AvatarUrl);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        User user = await LoadCurrentUserAsync(cancellationToken);
        await passwordConfirmation.ConfirmAsync(user, request.CurrentPassword, nameof(ChangePasswordRequest.CurrentPassword));

        // Also renews the security stamp, which cancels any password reset link already sent.
        IdentityResult result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            throw IdentityErrorMapper.ToValidationException(result, nameof(ChangePasswordRequest.NewPassword));
        }
    }

    public async Task<UserSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        ToSettings(await LoadCurrentUserAsync(cancellationToken));

    public async Task<UserSettings> UpdateSettingsAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        User user = await LoadCurrentUserAsync(cancellationToken);

        user.PreferredLanguage = settings.Language;
        user.NotifyOnNewMessage = settings.NotifyOnNewMessage;
        user.NotifyOnSimilarItem = settings.NotifyOnSimilarItem;
        user.NotifyOnPostUpdate = settings.NotifyOnPostUpdate;
        await db.SaveChangesAsync(cancellationToken);

        return ToSettings(user);
    }

    public async Task DeleteAccountAsync(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        User user = await LoadCurrentUserAsync(cancellationToken);
        await passwordConfirmation.ConfirmAsync(user, request.Password, nameof(DeleteAccountRequest.Password));

        await accountDeletion.DeleteAsync(user, cancellationToken);
    }

    /// <summary>Tracked, so changes are saved by SaveChanges. Deleted accounts are already refused at sign-in.</summary>
    private async Task<User> LoadCurrentUserAsync(CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        return await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken)
            ?? throw new UnauthorizedException("You must be signed in.");
    }

    private async Task<ProfileResponse> ToResponseAsync(User user)
    {
        UserRole role = await userManager.IsInRoleAsync(user, Roles.Admin) ? UserRole.Admin : UserRole.User;
        return new ProfileResponse(user.Id, user.FullName, user.Email!, user.UserType, role, user.AvatarUrl, user.CreatedAt);
    }

    private static UserSettings ToSettings(User user) => new()
    {
        Language = user.PreferredLanguage,
        NotifyOnNewMessage = user.NotifyOnNewMessage,
        NotifyOnSimilarItem = user.NotifyOnSimilarItem,
        NotifyOnPostUpdate = user.NotifyOnPostUpdate
    };
}
