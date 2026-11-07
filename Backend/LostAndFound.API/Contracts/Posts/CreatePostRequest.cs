using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Contracts.Posts;

public sealed record CreatePostRequest : PostDetails
{
    public required PostType Type { get; init; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (ValidationResult result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (Type == PostType.Lost && (HandedToSecurity || VerificationDetails is not null))
        {
            yield return new ValidationResult(
                "Only found posts can be handed to security or have verification details.",
                [nameof(HandedToSecurity), nameof(VerificationDetails)]);
        }
    }
}
