using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record UpdatePostRequest
{
    public required ItemCategory Category { get; init; }

    [Required, StringLength(2000, MinimumLength = 10)]
    public required string Description { get; init; }

    [StringLength(50)]
    public string? Color { get; init; }

    [Required]
    public required LocationDto Location { get; init; }

    public required DateOnly DateOccurred { get; init; }

    [StringLength(300)]
    public string? CurrentItemLocation { get; init; }

    [MaxLength(5)]
    public IReadOnlyList<string>? VerificationQuestions { get; init; }

    [StringLength(1000)]
    public string? PrivateDetails { get; init; }
}
