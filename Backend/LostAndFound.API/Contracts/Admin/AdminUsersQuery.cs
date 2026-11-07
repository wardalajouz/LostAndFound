using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Contracts.Admin;

public sealed record AdminUsersQuery : PagedQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    public UserType? UserType { get; init; }

    public AccountStatus? Status { get; init; }
}
