using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Services.Posts;

namespace LostAndFound.Api.Services.Admin;

/// <summary>Every admin action needs a real reason; spaces alone do not count.</summary>
internal static class ModerationReason
{
    public const int MinimumLength = 3;

    public static string Require(string reason, string field)
    {
        string? trimmed = OptionalText.Normalize(reason);
        return trimmed is { Length: >= MinimumLength }
            ? trimmed
            : throw new RequestValidationException(field, $"Give a reason of at least {MinimumLength} characters.");
    }
}
