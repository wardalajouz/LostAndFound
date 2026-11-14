using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Auth;

/// <summary>Email and Token come from the reset link sent by forgot-password.</summary>
public sealed record ResetPasswordRequest
{
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    [Required]
    public required string Token { get; init; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string NewPassword { get; init; }

    [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmNewPassword { get; init; }
}
