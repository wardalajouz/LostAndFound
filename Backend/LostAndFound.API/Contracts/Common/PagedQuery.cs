using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Common;

public record PagedQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 20;
}
