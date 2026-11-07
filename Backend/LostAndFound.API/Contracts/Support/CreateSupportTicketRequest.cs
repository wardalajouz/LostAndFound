using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Support;

/// <summary>Guests can submit this too, so name and email are always required.</summary>
public sealed record CreateSupportTicketRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(256)]
    public required string Email { get; init; }

    [Required, StringLength(150, MinimumLength = 3)]
    public required string Subject { get; init; }

    [Required, StringLength(4000, MinimumLength = 10)]
    public required string Message { get; init; }
}
