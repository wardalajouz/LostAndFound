using System.ComponentModel.DataAnnotations;
namespace LostAndFound.Api.Contracts.Auth;

public sealed record RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }
    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }
    [Required, Phone, StringLength(20)]
    public required string PhoneNumber { get; init; }
    [Required, StringLength(128, MinimumLength = 8)]
    public required string Password { get; init; }
}