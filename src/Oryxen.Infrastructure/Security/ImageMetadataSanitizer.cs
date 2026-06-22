using System.Text.RegularExpressions;
using Oryxen.Application.Common.Interfaces;

namespace Oryxen.Infrastructure.Security;

public sealed class ImageMetadataSanitizer : IImageMetadataSanitizer
{
    private static readonly byte[] ExifHeader = [0x45, 0x78, 0x69, 0x66, 0x00, 0x00];

    private static readonly byte[] GpsIfdTag = [0x88, 0x25];

    public Stream StripExifMetadata(Stream imageStream)
    {
        var ms = new MemoryStream();
        imageStream.CopyTo(ms);
        ms.Position = 0;

        var buffer = ms.ToArray();
        ms.Dispose();

        if (buffer.Length < 20)
            return new MemoryStream(buffer);

        if (!IsJpeg(buffer))
            return new MemoryStream(buffer);

        var cleaned = RemoveExifApp1Segment(buffer);

        return new MemoryStream(cleaned);
    }

    private static bool IsJpeg(byte[] data)
    {
        return data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8;
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
