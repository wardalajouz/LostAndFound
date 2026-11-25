using System.ComponentModel.DataAnnotations;
using LostAndFound.Api.Localization;

namespace LostAndFound.Api.Contracts.Posts;

/// <summary>Fields shared by create and update post requests.</summary>
public abstract record PostDetails : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string ItemName { get; init; }

    public required ItemCategory Category { get; init; }

    [Required, StringLength(50)]
    public required string Color { get; init; }

    [StringLength(50)]
    public string? Brand { get; init; }

    [Required, StringLength(2000, MinimumLength = 10)]
    public required string Description { get; init; }

    [Range(1, int.MaxValue)]
    public required int CampusLocationId { get; init; }

    [StringLength(300)]
    public string? AdditionalLocationDetails { get; init; }

    public required DateOnly DateOccurred { get; init; }

    /// <summary>Found posts only.</summary>
    public bool HandedToSecurity { get; init; }

    /// <summary>Found posts only. Never returned publicly.</summary>
    [StringLength(1000)]
    public string? VerificationDetails { get; init; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        DateOnly today = CampusTime.Today(DateTimeOffset.UtcNow);

        if (DateOccurred > today)
        {
            yield return new ValidationResult("Date cannot be in the future.", [nameof(DateOccurred)]);
        }
    }
}
