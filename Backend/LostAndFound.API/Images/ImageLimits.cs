namespace LostAndFound.Api.Images;

public static class ImageLimits
{
    public const int MaxFileBytes = 5 * 1024 * 1024;

    public const int MaxImagesPerPost = 5;

    /// <summary>Rejects decompression bombs before any pixels are decoded (about a 50-megapixel photo).</summary>
    public const long MaxSourcePixels = 50_000_000;

    /// <summary>Longest side after resizing. Enough for a full-screen view, much smaller than phone photos.</summary>
    public const int MaxOutputDimension = 1920;

    public const int WebpQuality = 80;

    /// <summary>Request body limits, with room for the multipart framing.</summary>
    public const long PostImagesRequestBytes = (MaxImagesPerPost * (long)MaxFileBytes) + (1024 * 1024);

    public const long AvatarRequestBytes = MaxFileBytes + (1024 * 1024);
}
