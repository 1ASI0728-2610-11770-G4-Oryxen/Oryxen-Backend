using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.Security;

public sealed class ImageMetadataSanitizer : IImageMetadataSanitizer
{
    private static readonly byte[] ExifHeader = [0x45, 0x78, 0x69, 0x66, 0x00, 0x00];

    // Ancillary PNG chunks that can carry EXIF/GPS or free-form metadata. Stripped destructively.
    private static readonly HashSet<string> PngMetadataChunks = ["eXIf", "tEXt", "zTXt", "iTXt", "tIME"];

    public Stream StripExifMetadata(Stream imageStream)
    {
        var ms = new MemoryStream();
        imageStream.CopyTo(ms);
        ms.Position = 0;

        var buffer = ms.ToArray();
        ms.Dispose();

        if (buffer.Length < 20)
            return new MemoryStream(buffer);

        if (IsJpeg(buffer))
            return new MemoryStream(RemoveExifApp1Segment(buffer));

        if (IsPng(buffer))
            return new MemoryStream(RemovePngMetadataChunks(buffer));

        return new MemoryStream(buffer);
    }

    private static bool IsJpeg(byte[] data)
    {
        return data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;
    }

    private static bool IsPng(byte[] data)
    {
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        return data.Length >= 8 &&
               data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
               data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
    }

    /// <summary>
    /// Rebuilds a PNG keeping only structural chunks and dropping ancillary metadata chunks
    /// (eXIf/tEXt/zTXt/iTXt/tIME), which can carry GPS coordinates or camera markers.
    /// </summary>
    private static byte[] RemovePngMetadataChunks(byte[] data)
    {
        var result = new List<byte>(data.Length);
        result.AddRange(data[..8]); // 8-byte PNG signature

        var pos = 8;
        while (pos + 8 <= data.Length)
        {
            var length = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
            if (length < 0)
            {
                break;
            }

            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            var chunkTotal = 12 + length; // length(4) + type(4) + data + CRC(4)

            if (pos + chunkTotal > data.Length)
            {
                // Truncated/corrupt tail: copy what's left and stop.
                result.AddRange(data[pos..]);
                break;
            }

            if (!PngMetadataChunks.Contains(type))
            {
                result.AddRange(data[pos..(pos + chunkTotal)]);
            }

            pos += chunkTotal;

            if (type == "IEND")
            {
                break;
            }
        }

        return result.ToArray();
    }

    private static byte[] RemoveExifApp1Segment(byte[] data)
    {
        var result = new List<byte>();
        result.Add(data[0]);
        result.Add(data[1]);

        var pos = 2;
        while (pos < data.Length - 1)
        {
            if (data[pos] != 0xFF)
                break;

            var marker = data[pos + 1];

            if (marker == 0xDA)
            {
                result.AddRange(data[pos..]);
                break;
            }

            if (marker == 0xE1 && pos + 4 < data.Length)
            {
                var length = (data[pos + 2] << 8) | data[pos + 3];

                var segmentStart = pos + 4;
                var segmentLength = Math.Min(length - 2, data.Length - segmentStart);

                if (segmentLength >= 6 &&
                    data[segmentStart] == ExifHeader[0] &&
                    data[segmentStart + 1] == ExifHeader[1] &&
                    data[segmentStart + 2] == ExifHeader[2] &&
                    data[segmentStart + 3] == ExifHeader[3] &&
                    data[segmentStart + 4] == ExifHeader[4] &&
                    data[segmentStart + 5] == ExifHeader[5])
                {
                    pos += 2 + length;
                    continue;
                }
            }

            result.Add(data[pos]);
            result.Add(data[pos + 1]);

            if (marker is 0xD8 or 0xD9)
            {
                pos += 2;
                continue;
            }

            if (pos + 3 < data.Length)
            {
                var segLen = (data[pos + 2] << 8) | data[pos + 3];
                var segEnd = pos + 2 + segLen;
                var segEndClamped = Math.Min(segEnd, data.Length);
                result.AddRange(data[pos..Math.Min(segEndClamped, data.Length)]);
                pos = segEndClamped;
                continue;
            }

            break;
        }

        return result.ToArray();
    }
}
