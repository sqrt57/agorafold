using AgoraFold.Core.Common;
using AgoraFold.Core.Entities;

namespace AgoraFold.Core.Services;

public interface IListingService
{
    /// <summary>Paginated, optionally category- and keyword-filtered listing browse. Each item's <see cref="Listing.Images"/> contains at most its thumbnail.</summary>
    Task<PagedResult<Listing>> BrowseAsync(int? categoryId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Full listing detail: Category, Owner, and the full ordered image gallery.</summary>
    Task<Listing> GetDetailAsync(int listingId, CancellationToken cancellationToken = default);

    /// <summary>All listings owned by <paramref name="ownerId"/>, newest first, unpaged.</summary>
    Task<IReadOnlyList<Listing>> GetMyListingsAsync(string ownerId, CancellationToken cancellationToken = default);

    Task<Listing> CreateAsync(string ownerId, ListingEditRequest request, CancellationToken cancellationToken = default);

    Task<Listing> UpdateAsync(int listingId, string requestingUserId, ListingEditRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int listingId, string requestingUserId, CancellationToken cancellationToken = default);
}
