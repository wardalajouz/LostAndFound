using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Profile;

public sealed record ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string NewPassword { get; init; }

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmNewPassword { get; init; }
}
