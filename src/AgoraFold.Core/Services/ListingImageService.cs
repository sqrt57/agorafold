using AgoraFold.Core.Entities;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Core.Services;

public sealed class ListingImageService(AppDbContext db, IListingImageStorage imageStorage) : IListingImageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const int MaxImagesPerListing = 8;

    public async Task<IReadOnlyList<ListingImage>> AddImagesAsync(int listingId, string requestingUserId, IReadOnlyList<ListingImageUpload> uploads, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), listingId);

        if (listing.OwnerId != requestingUserId)
        {
            throw new ForbiddenException("Only the listing owner may add images to this listing.");
        }

        if (uploads.Count == 0)
        {
            throw new ValidationException("At least one image is required.");
        }

        if (listing.Images.Count + uploads.Count > MaxImagesPerListing)
        {
            throw new ValidationException($"A listing can have at most {MaxImagesPerListing} images.");
        }

        var errors = new List<string>();
        var detectedExtensions = new string[uploads.Count];

        for (var i = 0; i < uploads.Count; i++)
        {
            var upload = uploads[i];

            if (upload.Length > MaxFileSizeBytes)
            {
                errors.Add($"{upload.FileName} exceeds the 5 MB limit.");
                continue;
            }

            var header = new byte[ImageSignatureDetector.MinHeaderBytes];
            upload.Content.Seek(0, SeekOrigin.Begin);
            var bytesRead = await ReadFullyAsync(upload.Content, header, cancellationToken);
            upload.Content.Seek(0, SeekOrigin.Begin);

            if (!ImageSignatureDetector.TryDetectExtension(header.AsSpan(0, bytesRead), out var extension))
            {
                errors.Add($"{upload.FileName} is not a supported image type.");
                continue;
            }

            detectedExtensions[i] = extension;
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var nextSortOrder = listing.Images.Count == 0 ? 0 : listing.Images.Max(i => i.SortOrder) + 1;
        var newImages = new List<ListingImage>(uploads.Count);

        for (var i = 0; i < uploads.Count; i++)
        {
            var path = await imageStorage.SaveAsync(listingId, uploads[i].Content, detectedExtensions[i], cancellationToken);

            var image = new ListingImage
            {
                ListingId = listingId,
                Path = path,
                SortOrder = nextSortOrder++,
            };

            db.ListingImages.Add(image);
            newImages.Add(image);
        }

        await db.SaveChangesAsync(cancellationToken);

        return newImages;
    }

    public async Task DeleteImageAsync(int imageId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        var image = await db.ListingImages
            .Include(i => i.Listing)
            .FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken)
            ?? throw new NotFoundException(nameof(ListingImage), imageId);

        if (image.Listing.OwnerId != requestingUserId)
        {
            throw new ForbiddenException("Only the listing owner may delete this image.");
        }

        await imageStorage.DeleteAsync(image.Path, cancellationToken);

        db.ListingImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}
