namespace AgoraFold.Core.Services;

/// <summary>
/// An image to add to a listing. <paramref name="Content"/> must be a seekable stream —
/// validation reads its header, then rewinds it before storage reads the rest.
/// <paramref name="FileName"/> is used only for error-message context, never trusted for type detection.
/// </summary>
public sealed record ListingImageUpload(Stream Content, string FileName, long Length);
