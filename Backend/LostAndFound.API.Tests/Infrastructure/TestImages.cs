using System.Buffers.Binary;
using System.Text;
using SkiaSharp;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>Builds real image files, and deliberately broken ones, for upload tests.</summary>
public static class TestImages
{
    public static readonly SKColor Red = new(255, 0, 0);
    public static readonly SKColor Green = new(0, 255, 0);
    public static readonly SKColor Blue = new(0, 0, 255);
    public static readonly SKColor Yellow = new(255, 255, 0);

    public static byte[] Png(int width = 64, int height = 48, SKColor? color = null) =>
        Encode(Solid(width, height, color ?? Blue), SKEncodedImageFormat.Png);

    public static byte[] Jpeg(int width = 64, int height = 48, SKColor? color = null) =>
        Encode(Solid(width, height, color ?? Red), SKEncodedImageFormat.Jpeg);

    public static byte[] Webp(int width = 64, int height = 48, SKColor? color = null) =>
        Encode(Solid(width, height, color ?? Green), SKEncodedImageFormat.Webp);

    public static byte[] TransparentPng(int width = 32, int height = 32) =>
        Encode(Solid(width, height, SKColors.Transparent), SKEncodedImageFormat.Png);

    /// <summary>40×20 image: red top-left, green top-right, blue bottom-left, yellow bottom-right.</summary>
    public static byte[] QuadrantJpeg()
    {
        using var bitmap = new SKBitmap(40, 20);
        using (var canvas = new SKCanvas(bitmap))
        {
            Fill(canvas, 0, 0, Red);
            Fill(canvas, 20, 0, Green);
            Fill(canvas, 0, 10, Blue);
            Fill(canvas, 20, 10, Yellow);
        }

        return Encode(bitmap, SKEncodedImageFormat.Jpeg, quality: 100);

        static void Fill(SKCanvas canvas, int x, int y, SKColor color)
        {
            using var paint = new SKPaint { Color = color };
            canvas.DrawRect(x, y, 20, 10, paint);
        }
    }

    /// <summary>Adds an EXIF block with an orientation, a description, and a GPS location to a JPEG.</summary>
    public static byte[] WithExif(byte[] jpeg, ushort orientation, string description)
    {
        byte[] exif = BuildExif(orientation, description);
        var segment = new byte[4 + exif.Length];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2), (ushort)(exif.Length + 2));
        exif.CopyTo(segment, 4);

        // Insert right after the SOI marker (FF D8).
        return [.. jpeg[..2], .. segment, .. jpeg[2..]];
    }

    public static byte[] Gif1x1() => Convert.FromBase64String("R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

    public static byte[] Bmp1x1() => Convert.FromBase64String("Qk06AAAAAAAAADYAAAAoAAAAAQAAAAEAAAABABgAAAAAAAQAAAATCwAAEwsAAAAAAAAAAAAA/wAAAA==");

    /// <summary>A PNG whose header claims 20000×20000 pixels: decoding it would need 1.6 GB of memory.</summary>
    public static byte[] PngClaimingHugeSize()
    {
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0), 20_000);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), 20_000);
        ihdr[8] = 8;
        ihdr[9] = 2;

        byte[] tinyIdat = [0x78, 0x9C, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01];
        return [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. Chunk("IHDR", ihdr), .. Chunk("IDAT", tinyIdat), .. Chunk("IEND", [])];
    }

    public static SKBitmap Decode(byte[] data) =>
        SKBitmap.Decode(data) ?? throw new InvalidOperationException("Not a decodable image.");

    public static bool IsWebp(byte[] data) =>
        data.Length > 12 && Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && Encoding.ASCII.GetString(data, 8, 4) == "WEBP";

    private static SKBitmap Solid(int width, int height, SKColor color)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(color);
        return bitmap;
    }

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality = 90)
    {
        using (bitmap)
        using (SKImage image = SKImage.FromBitmap(bitmap))
        using (SKData data = image.Encode(format, quality))
        {
            return data.ToArray();
        }
    }

    private static byte[] BuildExif(ushort orientation, string description)
    {
        // Little-endian TIFF: IFD0 has Orientation, ImageDescription, and a pointer to a GPS IFD.
        byte[] text = Encoding.ASCII.GetBytes(description + "\0");
        const int ifd0Offset = 8;
        const int ifd0Entries = 3;
        int ifd0Size = 2 + (ifd0Entries * 12) + 4;
        int gpsOffset = ifd0Offset + ifd0Size;
        int gpsSize = 2 + 12 + 4;
        int textOffset = gpsOffset + gpsSize;

        var tiff = new byte[textOffset + text.Length];
        "II*\0"u8.CopyTo(tiff);
        BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(4), ifd0Offset);

        Span<byte> ifd = tiff.AsSpan(ifd0Offset);
        BinaryPrimitives.WriteUInt16LittleEndian(ifd, ifd0Entries);
        WriteEntry(ifd[2..], tag: 0x010E, type: 2, count: (uint)text.Length, value: (uint)textOffset);
        WriteEntry(ifd[14..], tag: 0x0112, type: 3, count: 1, value: orientation);
        WriteEntry(ifd[26..], tag: 0x8825, type: 4, count: 1, value: (uint)gpsOffset);

        Span<byte> gps = tiff.AsSpan(gpsOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(gps, 1);
        WriteEntry(gps[2..], tag: 0x0001, type: 2, count: 2, value: 'N');

        text.CopyTo(tiff, textOffset);
        return [.. "Exif\0\0"u8, .. tiff];

        static void WriteEntry(Span<byte> span, ushort tag, ushort type, uint count, uint value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(span, tag);
            BinaryPrimitives.WriteUInt16LittleEndian(span[2..], type);
            BinaryPrimitives.WriteUInt32LittleEndian(span[4..], count);
            BinaryPrimitives.WriteUInt32LittleEndian(span[8..], value);
        }
    }

    private static byte[] Chunk(string type, byte[] data)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        var chunk = new byte[12 + data.Length];
        BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint)data.Length);
        typeBytes.CopyTo(chunk, 4);
        data.CopyTo(chunk, 8);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + data.Length), Crc32([.. typeBytes, .. data]));
        return chunk;
    }

    private static uint Crc32(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return ~crc;
    }
}
