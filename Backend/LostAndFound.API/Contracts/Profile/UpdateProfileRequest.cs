using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Profile;

public sealed record UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    public required UserType UserType { get; init; }

    /// <summary>Required only when <see cref="Email"/> changes, so a stolen token cannot take over the account.</summary>
    [StringLength(128)]
    public string? CurrentPassword { get; init; }
}
