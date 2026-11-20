using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Images;
using LostAndFound.Api.Services.Posts;
using LostAndFound.Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

public sealed class ProfileService(
    AppDbContext db,
    ICurrentUser currentUser,
    IImageProcessor imageProcessor,
    IFileStorage storage,
    ILogger<ProfileService> logger) : IProfileService
{
    private const string AvatarFolder = "avatars";

    public Task<ProfileResponse> GetAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    /// <summary>Replaces the profile picture. The previous file is removed once the new one is saved.</summary>
    public async Task<AvatarResponse> UploadAvatarAsync(UploadAvatarRequest request, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        User user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken)
            ?? throw new UnauthorizedException("You must be signed in.");

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

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UserSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UserSettings> UpdateSettingsAsync(UserSettings settings, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAccountAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
