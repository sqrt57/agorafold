namespace AgoraFold.Core.Services;

/// <summary>
/// Detects an image's true type from its file signature (magic bytes), independent of
/// any client-supplied file name or content-type header.
/// </summary>
internal static class ImageSignatureDetector
{
    internal const int MinHeaderBytes = 12;

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffSignature = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebpSignature = [0x57, 0x45, 0x42, 0x50];

    internal static bool TryDetectExtension(ReadOnlySpan<byte> header, out string extension)
    {
        if (header.Length >= JpegSignature.Length && header[..JpegSignature.Length].SequenceEqual(JpegSignature))
        {
            extension = ".jpg";
            return true;
        }

        if (header.Length >= PngSignature.Length && header[..PngSignature.Length].SequenceEqual(PngSignature))
        {
            extension = ".png";
            return true;
        }

        if (header.Length >= 12
            && header[..RiffSignature.Length].SequenceEqual(RiffSignature)
            && header[8..12].SequenceEqual(WebpSignature))
        {
            extension = ".webp";
            return true;
        }

        extension = "";
        return false;
    }
}
