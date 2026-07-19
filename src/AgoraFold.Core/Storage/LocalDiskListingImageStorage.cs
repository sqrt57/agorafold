using Microsoft.Extensions.Options;

namespace AgoraFold.Core.Storage;

public sealed class LocalDiskListingImageStorage(IOptions<ListingImageStorageOptions> options) : IListingImageStorage
{
    public async Task<string> SaveAsync(int listingId, Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var rootPath = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException(
                $"{nameof(ListingImageStorageOptions)}.{nameof(ListingImageStorageOptions.RootPath)} is not configured.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"{listingId}/{fileName}";

        var directory = Path.Combine(rootPath, listingId.ToString());
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);
        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var rootPath = options.Value.RootPath;
        var fullPath = Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
