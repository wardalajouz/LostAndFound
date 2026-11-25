namespace LostAndFound.Api.Services.Search;

/// <summary>
/// Builds SQL LIKE patterns that match the user's text literally: %, _, and [ are searched
/// as normal characters instead of acting as wildcards. Pass <see cref="EscapeCharacter"/> to EF.Functions.Like.
/// </summary>
internal static class LikeSearch
{
    public const string EscapeCharacter = "\\";

    /// <summary>"%text%" with the text escaped, for a "contains" search.</summary>
    public static string ContainsPattern(string text) => $"%{Escape(text.Trim())}%";

    private static string Escape(string text) => text
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal)
        .Replace("[", @"\[", StringComparison.Ordinal);
}
