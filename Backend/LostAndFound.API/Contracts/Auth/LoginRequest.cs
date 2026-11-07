using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Auth;

public sealed record LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
