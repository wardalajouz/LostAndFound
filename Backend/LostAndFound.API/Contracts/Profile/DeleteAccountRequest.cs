using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Profile;

/// <summary>Deleting an account cannot be undone, so the password is asked for again.</summary>
public sealed record DeleteAccountRequest
{
    [Required, StringLength(128)]
    public required string Password { get; init; }
}
