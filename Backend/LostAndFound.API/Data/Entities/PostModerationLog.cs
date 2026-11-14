using LostAndFound.Api.Contracts.Admin;

namespace LostAndFound.Api.Data.Entities;

public sealed class PostModerationLog
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Post Post { get; set; } = null!;

    public Guid AdminId { get; set; }

    public User Admin { get; set; } = null!;

    /// <summary>The visibility the admin set.</summary>
    public PostVisibility Visibility { get; set; }

    public required string Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
