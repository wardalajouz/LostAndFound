using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Auth;

public sealed record RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    public required UserType UserType { get; init; }

    [Required, StringLength(128, MinimumLength = 8)]
    public required string Password { get; init; }

    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; init; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions.")]
    public required bool AcceptTerms { get; init; }
}
