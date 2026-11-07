using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record BrowsePostsQuery : PagedQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    public PostType? Type { get; init; }

    public ItemCategory? Category { get; init; }

    [Range(1, int.MaxValue)]
    public int? CampusLocationId { get; init; }

    public DateOnly? Date { get; init; }

    public PostSort Sort { get; init; } = PostSort.Newest;
}
