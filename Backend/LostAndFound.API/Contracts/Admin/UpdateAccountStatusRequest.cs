using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record UpdateAccountStatusRequest
{
    public required AccountStatus Status { get; init; }

    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }
}
