namespace LostAndFound.Api.Services.Posts;

internal static class OptionalText
{
    /// <summary>Trims the value and stores blank input as null.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
