using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Contracts.Admin;

/// <summary>
/// Also serves the "Resolved Cases" page (Status=Resolved)
/// and the "Security Handover" page (HandedToSecurity=true).
/// </summary>
public sealed record AdminPostsQuery : PagedQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    public PostType? Type { get; init; }

    public ItemCategory? Category { get; init; }

    public PostStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int? CampusLocationId { get; init; }

    public bool? HandedToSecurity { get; init; }

    public PostVisibility? Visibility { get; init; }

    public PostSort Sort { get; init; } = PostSort.Newest;
}
