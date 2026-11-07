using LostAndFound.Api.Contracts.Profile;

namespace LostAndFound.Api.Services;

public sealed class ProfileService : IProfileService
{
    public Task<ProfileResponse> GetAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<AvatarResponse> UploadAvatarAsync(UploadAvatarRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UserSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UserSettings> UpdateSettingsAsync(UserSettings settings, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAccountAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
