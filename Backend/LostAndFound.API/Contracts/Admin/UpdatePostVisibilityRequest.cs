using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record UpdatePostVisibilityRequest
{
    public required PostVisibility Visibility { get; init; }

    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }
}
