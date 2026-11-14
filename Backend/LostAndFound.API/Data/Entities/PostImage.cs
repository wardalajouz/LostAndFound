namespace LostAndFound.Api.Data.Entities;

public sealed class PostImage
{
    public Guid Id { get; set; }

    public Guid PostId { get; set; }

    public Post Post { get; set; } = null!;

    /// <summary>File path or blob name. The public URL is built from it.</summary>
    public required string StorageKey { get; set; }

    /// <summary>0 is the thumbnail.</summary>
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
