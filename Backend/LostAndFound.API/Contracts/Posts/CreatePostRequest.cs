using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record CreatePostRequest
{
    public required PostType Type { get; init; }

    public required ItemCategory Category { get; init; }
    [Required, StringLength(2000, MinimumLength = 10)]
    public required string Description { get; init; }

    [StringLength(50)]
    public string? Color { get; init; }

    [Required]
    public required LocationDto Location { get; init; }

    public required DateOnly DateOccurred { get; init; }

    /// <summary>Found posts only: where the item is kept now.</summary>
    [StringLength(300)]
    public string? CurrentItemLocation { get; init; }

    /// <summary>Found posts only. Never returned publicly.</summary>
    [MaxLength(5)]
    public IReadOnlyList<string>? VerificationQuestions { get; init; }

    /// <summary>Found posts only. Never returned publicly.</summary>
    [StringLength(1000)]
    public string? PrivateDetails { get; init; }
}
