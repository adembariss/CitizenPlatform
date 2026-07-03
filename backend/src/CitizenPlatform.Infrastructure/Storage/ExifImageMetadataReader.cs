using System.Globalization;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.ValueObjects;

namespace CitizenPlatform.Infrastructure.Storage;

public sealed class ExifImageMetadataReader : IImageMetadataReader
{
    private const ushort ExifIfdPointerTag = 0x8769;
    private const ushort GpsIfdPointerTag = 0x8825;
    private const ushort DateTimeOriginalTag = 0x9003;
    private const ushort GpsLatitudeRefTag = 0x0001;
    private const ushort GpsLatitudeTag = 0x0002;
    private const ushort GpsLongitudeRefTag = 0x0003;
    private const ushort GpsLongitudeTag = 0x0004;

    public async Task<ImageMetadata> ReadAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ImageMetadata.Empty;
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await content.CopyToAsync(memoryStream, cancellationToken);
            return ReadJpegExif(memoryStream.ToArray());
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
        {
            return ImageMetadata.Empty;
        }
    }

    private static ImageMetadata ReadJpegExif(byte[] jpegBytes)
    {
        if (jpegBytes.Length < 4 || jpegBytes[0] != 0xFF || jpegBytes[1] != 0xD8)
        {
            return ImageMetadata.Empty;
        }

        var position = 2;
        while (position + 4 <= jpegBytes.Length)
        {
            if (jpegBytes[position] != 0xFF)
            {
                break;
            }

            var marker = jpegBytes[position + 1];
            position += 2;

            if (marker is 0xDA or 0xD9)
            {
                break;
            }

            if (position + 2 > jpegBytes.Length)
            {
                break;
            }

            var segmentLength = ReadBigEndianUInt16(jpegBytes, position);
            if (segmentLength < 2 || position + segmentLength > jpegBytes.Length)
            {
                break;
            }

            var segmentStart = position + 2;
            var segmentSize = segmentLength - 2;
            if (marker == 0xE1 && HasExifHeader(jpegBytes, segmentStart, segmentSize))
            {
                return ReadTiffMetadata(jpegBytes, segmentStart + 6, segmentSize - 6);
            }

            position += segmentLength;
        }

        return ImageMetadata.Empty;
    }

    private static ImageMetadata ReadTiffMetadata(byte[] bytes, int tiffStart, int tiffLength)
    {
        if (tiffLength < 8 || tiffStart < 0 || tiffStart + tiffLength > bytes.Length)
        {
            return ImageMetadata.Empty;
        }

        var littleEndian = bytes[tiffStart] == 0x49 && bytes[tiffStart + 1] == 0x49;
        var bigEndian = bytes[tiffStart] == 0x4D && bytes[tiffStart + 1] == 0x4D;
        if (!littleEndian && !bigEndian)
        {
            return ImageMetadata.Empty;
        }

        if (ReadUInt16(bytes, tiffStart + 2, littleEndian) != 42)
        {
            return ImageMetadata.Empty;
        }

        var firstIfdOffset = ReadUInt32(bytes, tiffStart + 4, littleEndian);
        var ifd0 = ReadIfd(bytes, tiffStart, tiffLength, firstIfdOffset, littleEndian);

        DateTimeOffset? takenAt = null;
        if (TryReadUIntValue(ifd0, ExifIfdPointerTag, out var exifIfdOffset))
        {
            var exifIfd = ReadIfd(bytes, tiffStart, tiffLength, exifIfdOffset, littleEndian);
            takenAt = ReadTakenAt(exifIfd, bytes, tiffStart, tiffLength, littleEndian);
        }

        GeoCoordinate? gpsLocation = null;
        if (TryReadUIntValue(ifd0, GpsIfdPointerTag, out var gpsIfdOffset))
        {
            var gpsIfd = ReadIfd(bytes, tiffStart, tiffLength, gpsIfdOffset, littleEndian);
            gpsLocation = ReadGpsLocation(gpsIfd, bytes, tiffStart, tiffLength, littleEndian);
        }

        return gpsLocation is null && takenAt is null
            ? ImageMetadata.Empty
            : new ImageMetadata(gpsLocation, takenAt);
    }

    private static DateTimeOffset? ReadTakenAt(
        IReadOnlyDictionary<ushort, TiffEntry> exifIfd,
        byte[] bytes,
        int tiffStart,
        int tiffLength,
        bool littleEndian)
    {
        var value = ReadAscii(exifIfd, DateTimeOriginalTag, bytes, tiffStart, tiffLength, littleEndian);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParseExact(
            value,
            "yyyy:MM:dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc))
            : null;
    }

    private static GeoCoordinate? ReadGpsLocation(
        IReadOnlyDictionary<ushort, TiffEntry> gpsIfd,
        byte[] bytes,
        int tiffStart,
        int tiffLength,
        bool littleEndian)
    {
        var latitudeRef = ReadAscii(gpsIfd, GpsLatitudeRefTag, bytes, tiffStart, tiffLength, littleEndian);
        var longitudeRef = ReadAscii(gpsIfd, GpsLongitudeRefTag, bytes, tiffStart, tiffLength, littleEndian);
        var latitudeParts = ReadRationals(gpsIfd, GpsLatitudeTag, bytes, tiffStart, tiffLength, littleEndian);
        var longitudeParts = ReadRationals(gpsIfd, GpsLongitudeTag, bytes, tiffStart, tiffLength, littleEndian);

        if (string.IsNullOrWhiteSpace(latitudeRef)
            || string.IsNullOrWhiteSpace(longitudeRef)
            || latitudeParts.Count < 3
            || longitudeParts.Count < 3)
        {
            return null;
        }

        var latitude = ToDecimalDegrees(latitudeParts);
        var longitude = ToDecimalDegrees(longitudeParts);

        if (latitudeRef.StartsWith("S", StringComparison.OrdinalIgnoreCase))
        {
            latitude *= -1;
        }

        if (longitudeRef.StartsWith("W", StringComparison.OrdinalIgnoreCase))
        {
            longitude *= -1;
        }

        try
        {
            return new GeoCoordinate(latitude, longitude);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<ushort, TiffEntry> ReadIfd(
        byte[] bytes,
        int tiffStart,
        int tiffLength,
        uint ifdOffset,
        bool littleEndian)
    {
        var entries = new Dictionary<ushort, TiffEntry>();
        if (ifdOffset > int.MaxValue)
        {
            return entries;
        }

        var ifdPosition = tiffStart + (int)ifdOffset;
        if (ifdPosition < tiffStart || ifdPosition + 2 > tiffStart + tiffLength)
        {
            return entries;
        }

        var entryCount = ReadUInt16(bytes, ifdPosition, littleEndian);
        var entryPosition = ifdPosition + 2;
        for (var index = 0; index < entryCount; index++)
        {
            if (entryPosition + 12 > tiffStart + tiffLength)
            {
                break;
            }

            var tag = ReadUInt16(bytes, entryPosition, littleEndian);
            var type = ReadUInt16(bytes, entryPosition + 2, littleEndian);
            var count = ReadUInt32(bytes, entryPosition + 4, littleEndian);
            var valueOffset = ReadUInt32(bytes, entryPosition + 8, littleEndian);

            entries[tag] = new TiffEntry(tag, type, count, valueOffset, entryPosition + 8);
            entryPosition += 12;
        }

        return entries;
    }

    private static bool TryReadUIntValue(
        IReadOnlyDictionary<ushort, TiffEntry> entries,
        ushort tag,
        out uint value)
    {
        value = 0;
        if (!entries.TryGetValue(tag, out var entry) || entry.Count != 1)
        {
            return false;
        }

        value = entry.Type switch
        {
            3 => entry.ValueOffset & 0xFFFF,
            4 => entry.ValueOffset,
            _ => 0
        };

        return entry.Type is 3 or 4;
    }

    private static string? ReadAscii(
        IReadOnlyDictionary<ushort, TiffEntry> entries,
        ushort tag,
        byte[] bytes,
        int tiffStart,
        int tiffLength,
        bool littleEndian)
    {
        if (!entries.TryGetValue(tag, out var entry) || entry.Type != 2 || entry.Count == 0 || entry.Count > int.MaxValue)
        {
            return null;
        }

        var byteCount = (int)entry.Count;
        var valuePosition = byteCount <= 4
            ? entry.ValueFieldOffset
            : tiffStart + (int)entry.ValueOffset;

        if (valuePosition < tiffStart || valuePosition + byteCount > tiffStart + tiffLength)
        {
            return null;
        }

        var valueBytes = bytes.AsSpan(valuePosition, byteCount).ToArray();
        var nullIndex = Array.IndexOf(valueBytes, (byte)0);
        if (nullIndex >= 0)
        {
            valueBytes = valueBytes[..nullIndex];
        }

        return System.Text.Encoding.ASCII.GetString(valueBytes).Trim();
    }

    private static IReadOnlyList<double> ReadRationals(
        IReadOnlyDictionary<ushort, TiffEntry> entries,
        ushort tag,
        byte[] bytes,
        int tiffStart,
        int tiffLength,
        bool littleEndian)
    {
        if (!entries.TryGetValue(tag, out var entry) || entry.Type != 5 || entry.Count == 0 || entry.Count > 8)
        {
            return Array.Empty<double>();
        }

        var valuePosition = tiffStart + (int)entry.ValueOffset;
        var byteCount = (int)entry.Count * 8;
        if (valuePosition < tiffStart || valuePosition + byteCount > tiffStart + tiffLength)
        {
            return Array.Empty<double>();
        }

        var values = new List<double>((int)entry.Count);
        for (var index = 0; index < entry.Count; index++)
        {
            var offset = valuePosition + (index * 8);
            var numerator = ReadUInt32(bytes, offset, littleEndian);
            var denominator = ReadUInt32(bytes, offset + 4, littleEndian);
            values.Add(denominator == 0 ? 0 : numerator / (double)denominator);
        }

        return values;
    }

    private static double ToDecimalDegrees(IReadOnlyList<double> parts)
    {
        return parts[0] + (parts[1] / 60d) + (parts[2] / 3600d);
    }

    private static bool HasExifHeader(byte[] bytes, int segmentStart, int segmentSize)
    {
        return segmentSize >= 6
            && bytes[segmentStart] == 0x45
            && bytes[segmentStart + 1] == 0x78
            && bytes[segmentStart + 2] == 0x69
            && bytes[segmentStart + 3] == 0x66
            && bytes[segmentStart + 4] == 0x00
            && bytes[segmentStart + 5] == 0x00;
    }

    private static ushort ReadBigEndianUInt16(byte[] bytes, int offset)
    {
        return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    private static ushort ReadUInt16(byte[] bytes, int offset, bool littleEndian)
    {
        return littleEndian
            ? (ushort)(bytes[offset] | (bytes[offset + 1] << 8))
            : (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    private static uint ReadUInt32(byte[] bytes, int offset, bool littleEndian)
    {
        return littleEndian
            ? (uint)(bytes[offset]
                | (bytes[offset + 1] << 8)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 3] << 24))
            : (uint)((bytes[offset] << 24)
                | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8)
                | bytes[offset + 3]);
    }

    private readonly record struct TiffEntry(
        ushort Tag,
        ushort Type,
        uint Count,
        uint ValueOffset,
        int ValueFieldOffset);
}
