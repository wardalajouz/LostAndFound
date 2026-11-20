namespace LostAndFound.Api.Services.Posts;

/// <summary>Public URLs for stored images. The files are served from <see cref="PathPrefix"/>.</summary>
public static class ImageUrls
{
    public const string PathPrefix = "/uploads/";

    public static string For(string storageKey) => PathPrefix + storageKey;

    /// <summary>The storage key behind a URL produced by <see cref="For"/>, or null for anything else.</summary>
    public static string? KeyFrom(string? url) =>
        url is not null && url.StartsWith(PathPrefix, StringComparison.Ordinal) && url.Length > PathPrefix.Length
            ? url[PathPrefix.Length..]
            : null;
}
