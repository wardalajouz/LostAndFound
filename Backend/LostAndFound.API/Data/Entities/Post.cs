using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Posts;

namespace LostAndFound.Api.Data.Entities;

public sealed class Post
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public PostType Type { get; set; }

    public required string ItemName { get; set; }

    public ItemCategory Category { get; set; }

    public required string Color { get; set; }

    public string? Brand { get; set; }

    public required string Description { get; set; }

    public int CampusLocationId { get; set; }

    public CampusLocation CampusLocation { get; set; } = null!;

    public string? AdditionalLocationDetails { get; set; }

    public DateOnly DateOccurred { get; set; }

    /// <summary>Found posts only.</summary>
    public bool HandedToSecurity { get; set; }

    /// <summary>Found posts only. Never returned publicly.</summary>
    public string? VerificationDetails { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Active;

    /// <summary>Deleting a post sets this to Deleted; rows are never removed.</summary>
    public PostVisibility Visibility { get; set; } = PostVisibility.Visible;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public ICollection<PostImage> Images { get; } = [];
}
