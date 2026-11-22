using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Images;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await profileService.GetAsync(cancellationToken));
    }

    /// <summary>Changing the email also needs <c>currentPassword</c>; a wrong one is a 400 on that field. 409 if the email is taken.</summary>
    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> Update(
        UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        return Ok(await profileService.UpdateAsync(request, cancellationToken));
    }

    [HttpPut("avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageLimits.AvatarRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageLimits.AvatarRequestBytes)]
    [ProducesResponseType<AvatarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvatarResponse>> UploadAvatar(
        [FromForm] UploadAvatarRequest request, CancellationToken cancellationToken)
    {
        return Ok(await profileService.UploadAvatarAsync(request, cancellationToken));
    }

    /// <summary>A wrong current password is a 400 on <c>currentPassword</c>, and counts toward the login lockout.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await profileService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("settings")]
    [ProducesResponseType<UserSettings>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSettings>> GetSettings(CancellationToken cancellationToken)
    {
        return Ok(await profileService.GetSettingsAsync(cancellationToken));
    }

    [HttpPut("settings")]
    [ProducesResponseType<UserSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSettings>> UpdateSettings(
        UserSettings settings, CancellationToken cancellationToken)
    {
        return Ok(await profileService.UpdateSettingsAsync(settings, cancellationToken));
    }

    /// <summary>
    /// Permanently deletes the account after checking the password: personal data is removed,
    /// posts are hidden, notifications are deleted, and the user is signed out everywhere.
    /// Chats stay, showing "Deleted user", so the other person keeps their history.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        await profileService.DeleteAccountAsync(request, cancellationToken);
        return NoContent();
    }
}
