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
}
