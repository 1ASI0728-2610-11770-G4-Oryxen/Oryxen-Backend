namespace Oryxen.Application.Common.Interfaces;

public interface IImageMetadataSanitizer
{
    Stream StripExifMetadata(Stream imageStream);
}
