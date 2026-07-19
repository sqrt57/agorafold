namespace AgoraFold.Core.Storage;

/// <summary>
/// Persists listing image files. Implementations are agnostic of any particular
/// hosting model (e.g. ASP.NET's wwwroot) — the returned path is storage-relative only.
/// </summary>
public interface IListingImageStorage
{
    /// <summary>
    /// Saves <paramref name="content"/> (read from its current position to the end) under
    /// a new, server-generated file name using <paramref name="extension"/> (server-detected,
    /// e.g. ".jpg" — never a client-supplied one). Returns a storage-relative path to persist
    /// as <see cref="Entities.ListingImage.Path"/>.
    /// </summary>
    Task<string> SaveAsync(int listingId, Stream content, string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the file at <paramref name="relativePath"/> (as previously returned by
    /// <see cref="SaveAsync"/>). Idempotent — deleting a missing file is a no-op.
    /// </summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
