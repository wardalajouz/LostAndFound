using SkiaSharp;

namespace LostAndFound.Api.Images;

/// <summary>
/// Decoding the pixels and encoding a new file means only real images are stored, and nothing
/// from the original file survives: not EXIF/GPS location, not hidden data appended to the file.
/// </summary>
public sealed class SkiaImageProcessor : IImageProcessor
{
    private const string UnsupportedFormat = "Upload a JPEG, PNG, or WebP image.";

    private static readonly HashSet<SKEncodedImageFormat> AllowedFormats =
        [SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Png, SKEncodedImageFormat.Webp];

    public async Task<ProcessedImage> ProcessAsync(Stream source, CancellationToken cancellationToken)
    {
        byte[] data = await ReadWithLimitAsync(source, cancellationToken);
        return Process(data);
    }

    private static async Task<byte[]> ReadWithLimitAsync(Stream source, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > ImageLimits.MaxFileBytes)
            {
                throw new InvalidImageException($"Each image must be {ImageLimits.MaxFileBytes / (1024 * 1024)} MB or smaller.");
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.Length == 0 ? throw new InvalidImageException("The file is empty.") : buffer.ToArray();
    }

    private static ProcessedImage Process(byte[] data)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(data)) ?? throw new InvalidImageException(UnsupportedFormat);

        if (!AllowedFormats.Contains(codec.EncodedFormat))
        {
            throw new InvalidImageException(UnsupportedFormat);
        }

        // Header only: the size is checked before memory is allocated for the pixels.
        if ((long)codec.Info.Width * codec.Info.Height > ImageLimits.MaxSourcePixels)
        {
            throw new InvalidImageException("The image is too large. Use a photo of 50 megapixels or less.");
        }

        using SKBitmap decoded = Decode(codec);
        using SKBitmap upright = ApplyOrientation(decoded, codec.EncodedOrigin);
        using SKBitmap resized = ResizeToFit(upright);

        using SKImage image = SKImage.FromBitmap(resized);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Webp, ImageLimits.WebpQuality)
            ?? throw new InvalidOperationException("WebP encoding failed.");

        return new ProcessedImage(encoded.ToArray(), ".webp", resized.Width, resized.Height);
    }

    /// <summary>Decodes the first frame only, so animated images become still images.</summary>
    private static SKBitmap Decode(SKCodec codec)
    {
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);

        SKCodecResult result = codec.GetPixels(info, bitmap.GetPixels());
        if (result != SKCodecResult.Success)
        {
            bitmap.Dispose();
            throw new InvalidImageException("The image file is damaged or incomplete.");
        }

        return bitmap;
    }

    /// <summary>Phones store photos sideways and record the rotation in EXIF; this bakes it into the pixels.</summary>
    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return source.Copy();
        }

        int w = source.Width;
        int h = source.Height;
        bool swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        // x' = ScaleX*x + SkewX*y + TransX,  y' = SkewY*x + ScaleY*y + TransY
        SKMatrix matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity
        };

        var target = new SKBitmap(new SKImageInfo(swapsSides ? h : w, swapsSides ? w : h, source.ColorType, source.AlphaType));
        using var canvas = new SKCanvas(target);
        using SKImage image = SKImage.FromBitmap(source);
        canvas.SetMatrix(matrix);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return target;
    }

    private static SKBitmap ResizeToFit(SKBitmap source)
    {
        int longest = Math.Max(source.Width, source.Height);
        if (longest <= ImageLimits.MaxOutputDimension)
        {
            return source.Copy();
        }

        double scale = (double)ImageLimits.MaxOutputDimension / longest;
        var info = new SKImageInfo(
            Math.Max(1, (int)Math.Round(source.Width * scale)),
            Math.Max(1, (int)Math.Round(source.Height * scale)),
            source.ColorType,
            source.AlphaType);

        return source.Resize(info, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidOperationException("Resizing the image failed.");
    }
}
