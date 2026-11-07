using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Contracts.Common;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record MyPostsQuery : PagedQuery
{
    [StringLength(100)]
    public string? Search { get; init; }

    public PostType? Type { get; init; }

    public PostStatus? Status { get; init; }

    public PostSort Sort { get; init; } = PostSort.Newest;
}
