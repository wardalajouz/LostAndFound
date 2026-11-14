using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Auth;

public sealed record ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }
}
