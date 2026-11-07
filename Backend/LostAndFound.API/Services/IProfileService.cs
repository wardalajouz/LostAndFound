using LostAndFound.Api.Contracts.Profile;

namespace LostAndFound.Api.Services;

/// <summary>Account of the signed-in user.</summary>
public interface IProfileService
{
    Task<ProfileResponse> GetAsync(CancellationToken cancellationToken);

    Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken);

    Task<AvatarResponse> UploadAvatarAsync(UploadAvatarRequest request, CancellationToken cancellationToken);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);

    Task<UserSettings> GetSettingsAsync(CancellationToken cancellationToken);

    Task<UserSettings> UpdateSettingsAsync(UserSettings settings, CancellationToken cancellationToken);

    Task DeleteAccountAsync(CancellationToken cancellationToken);
}
