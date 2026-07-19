using AgoraFold.Core.Entities;

namespace AgoraFold.Core.Services;

public interface IListingImageService
{
    /// <summary>
    /// Validates the whole batch (type, size, and the 8-images-per-listing cap) before writing
    /// anything — either every upload is saved, or none are and a <see cref="Exceptions.ValidationException"/> is thrown.
    /// </summary>
    Task<IReadOnlyList<ListingImage>> AddImagesAsync(int listingId, string requestingUserId, IReadOnlyList<ListingImageUpload> uploads, CancellationToken cancellationToken = default);

    Task DeleteImageAsync(int imageId, string requestingUserId, CancellationToken cancellationToken = default);
}
