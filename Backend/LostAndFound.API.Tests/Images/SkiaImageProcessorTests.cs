using System.Text;
using LostAndFound.Api.Images;
using LostAndFound.Api.Tests.Infrastructure;
using SkiaSharp;

namespace LostAndFound.Api.Tests.Images;

public sealed class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    public static TheoryData<string> AcceptedFormats => new() { "png", "jpeg", "webp" };

    [Theory]
    [MemberData(nameof(AcceptedFormats))]
    public async Task Accepted_formats_are_re_encoded_as_webp_with_the_same_size(string format)
    {
        byte[] source = format switch
        {
            "png" => TestImages.Png(64, 48),
            "jpeg" => TestImages.Jpeg(64, 48),
            _ => TestImages.Webp(64, 48)
        };

        ProcessedImage result = await ProcessAsync(source);

        Assert.True(TestImages.IsWebp(result.Content));
        Assert.Equal(".webp", result.Extension);
        Assert.Equal((64, 48), (result.Width, result.Height));
        using SKBitmap decoded = TestImages.Decode(result.Content);
        Assert.Equal((64, 48), (decoded.Width, decoded.Height));
    }

    [Theory]
    [InlineData(3000, 1000, 1920, 640)]
    [InlineData(1000, 3000, 640, 1920)]
    [InlineData(1920, 1920, 1920, 1920)]
    [InlineData(1921, 10, 1920, 10)]
    public async Task Large_images_are_scaled_down_keeping_the_shape(int width, int height, int expectedWidth, int expectedHeight)
    {
        ProcessedImage result = await ProcessAsync(TestImages.Png(width, height));

        Assert.Equal((expectedWidth, expectedHeight), (result.Width, result.Height));
        using SKBitmap decoded = TestImages.Decode(result.Content);
        Assert.Equal((expectedWidth, expectedHeight), (decoded.Width, decoded.Height));
    }

    /// <summary>Expected corner colors after applying each EXIF orientation to the 40×20 quadrant image.</summary>
    public static TheoryData<ushort, int, int, string> Orientations => new()
    {
        { 1, 40, 20, "RGBY" },
        { 2, 40, 20, "GRYB" },
        { 3, 40, 20, "YBGR" },
        { 4, 40, 20, "BYRG" },
        { 5, 20, 40, "RBGY" },
        { 6, 20, 40, "BRYG" },
        { 7, 20, 40, "YGBR" },
        { 8, 20, 40, "GYRB" }
    };

    [Theory]
    [MemberData(nameof(Orientations))]
    public async Task Exif_orientation_is_applied_to_the_pixels(ushort orientation, int width, int height, string corners)
    {
        byte[] photo = TestImages.WithExif(TestImages.QuadrantJpeg(), orientation, "camera");

        ProcessedImage result = await ProcessAsync(photo);

        using SKBitmap decoded = TestImages.Decode(result.Content);
        Assert.Equal((width, height), (decoded.Width, decoded.Height));
        string actual = string.Concat(
            ColorName(decoded.GetPixel(3, 3)),
            ColorName(decoded.GetPixel(width - 4, 3)),
            ColorName(decoded.GetPixel(3, height - 4)),
            ColorName(decoded.GetPixel(width - 4, height - 4)));
        Assert.Equal(corners, actual);
    }

    [Fact]
    public async Task Exif_and_gps_metadata_are_removed()
    {
        byte[] photo = TestImages.WithExif(TestImages.QuadrantJpeg(), orientation: 1, description: "SECRET-HOME-ADDRESS");
        Assert.Contains("SECRET-HOME-ADDRESS", Encoding.ASCII.GetString(photo));

        ProcessedImage result = await ProcessAsync(photo);

        string output = Encoding.ASCII.GetString(result.Content);
        Assert.DoesNotContain("SECRET-HOME-ADDRESS", output);
        Assert.DoesNotContain("Exif", output);
        Assert.DoesNotContain("EXIF", output);
        Assert.DoesNotContain("XMP", output);
    }

    [Fact]
    public async Task Data_hidden_after_the_image_is_not_kept()
    {
        byte[] polyglot = [.. TestImages.Png(), .. "<script>alert('x')</script>"u8];

        ProcessedImage result = await ProcessAsync(polyglot);

        Assert.DoesNotContain("<script>", Encoding.ASCII.GetString(result.Content));
    }

    [Fact]
    public async Task Transparency_is_kept()
    {
        ProcessedImage result = await ProcessAsync(TestImages.TransparentPng());

        using SKBitmap decoded = TestImages.Decode(result.Content);
        Assert.Equal(0, decoded.GetPixel(5, 5).Alpha);
    }

    public static TheoryData<string, byte[]> RejectedFiles => new()
    {
        { "text renamed to .jpg", "this is not an image"u8.ToArray() },
        { "GIF", TestImages.Gif1x1() },
        { "BMP", TestImages.Bmp1x1() },
        { "PDF", "%PDF-1.4\n%âãÏÓ\n1 0 obj\n<<>>\nendobj\n"u8.ToArray() },
        { "SVG", "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray() },
        { "HTML", "<html><body>hi</body></html>"u8.ToArray() }
    };

    [Theory]
    [MemberData(nameof(RejectedFiles))]
    public async Task Files_that_are_not_jpeg_png_or_webp_are_rejected(string description, byte[] content)
    {
        InvalidImageException exception = await Assert.ThrowsAsync<InvalidImageException>(() => ProcessAsync(content));

        Assert.False(string.IsNullOrWhiteSpace(exception.Message), description);
    }

    [Fact]
    public async Task Empty_file_is_rejected()
    {
        InvalidImageException exception = await Assert.ThrowsAsync<InvalidImageException>(() => ProcessAsync([]));

        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public async Task File_over_5_mb_is_rejected_without_reading_it_all()
    {
        var stream = new CountingStream(new MemoryStream(new byte[20 * 1024 * 1024]));

        InvalidImageException exception = await Assert.ThrowsAsync<InvalidImageException>(
            () => _processor.ProcessAsync(stream, CancellationToken.None));

        Assert.Contains("5 MB", exception.Message);
        Assert.True(stream.BytesRead < 6 * 1024 * 1024, $"Read {stream.BytesRead} bytes");
    }

    [Fact]
    public async Task File_of_exactly_5_mb_is_not_rejected_for_its_size()
    {
        byte[] png = TestImages.Png();
        byte[] padded = [.. png, .. new byte[ImageLimits.MaxFileBytes - png.Length]];

        ProcessedImage result = await ProcessAsync(padded);

        Assert.True(TestImages.IsWebp(result.Content));
    }

    [Fact]
    public async Task Image_claiming_a_huge_size_is_rejected_before_decoding()
    {
        InvalidImageException exception = await Assert.ThrowsAsync<InvalidImageException>(
            () => ProcessAsync(TestImages.PngClaimingHugeSize()));

        Assert.Contains("too large", exception.Message);
    }

    [Fact]
    public async Task Truncated_image_is_rejected()
    {
        byte[] jpeg = TestImages.Jpeg(200, 200);

        await Assert.ThrowsAsync<InvalidImageException>(() => ProcessAsync(jpeg[..(jpeg.Length / 2)]));
    }

    [Fact]
    public async Task Cancellation_is_respected()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _processor.ProcessAsync(new MemoryStream(TestImages.Png()), cancelled.Token));
    }

    private Task<ProcessedImage> ProcessAsync(byte[] content) =>
        _processor.ProcessAsync(new MemoryStream(content), CancellationToken.None);

    /// <summary>Lossy encoding shifts colors slightly, so each pixel is matched to the nearest test color.</summary>
    private static char ColorName(SKColor color) => (color.Red > 128, color.Green > 128, color.Blue > 128) switch
    {
        (true, false, false) => 'R',
        (false, true, false) => 'G',
        (false, false, true) => 'B',
        (true, true, false) => 'Y',
        _ => '?'
    };

    private sealed class CountingStream(Stream inner) : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
