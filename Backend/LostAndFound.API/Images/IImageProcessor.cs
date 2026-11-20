namespace LostAndFound.Api.Images;

public interface IImageProcessor
{
    /// <summary>
    /// Validates an uploaded image and re-encodes it as WebP: upright, at most
    /// <see cref="ImageLimits.MaxOutputDimension"/> pixels on its longest side, with all metadata removed.
    /// </summary>
    /// <exception cref="InvalidImageException">The upload is not an acceptable image.</exception>
    Task<ProcessedImage> ProcessAsync(Stream source, CancellationToken cancellationToken);
}

public sealed record ProcessedImage(byte[] Content, string Extension, int Width, int Height);

/// <summary>The message is safe to show to the user.</summary>
public sealed class InvalidImageException(string message) : Exception(message);
