using AgoraFold.Core.Common;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Core.Services;

public sealed class ListingService(AppDbContext db, IListingImageStorage imageStorage) : IListingService
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    public async Task<PagedResult<Listing>> BrowseAsync(int? categoryId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Listings.AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(l => l.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(l => EF.Functions.ILike(l.Title, pattern) || EF.Functions.ILike(l.Description, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(l => l.Category)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Listing>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<Listing> GetDetailAsync(int listingId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.AsNoTracking()
            .Include(l => l.Category)
            .Include(l => l.Owner)
            .Include(l => l.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);

        return listing ?? throw new NotFoundException(nameof(Listing), listingId);
    }

    public async Task<IReadOnlyList<Listing>> GetMyListingsAsync(string ownerId, CancellationToken cancellationToken = default) =>
        await db.Listings.AsNoTracking()
            .Where(l => l.OwnerId == ownerId)
            .Include(l => l.Category)
            .Include(l => l.Images.OrderBy(i => i.SortOrder).Take(1))
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Listing> CreateAsync(string ownerId, ListingEditRequest request, CancellationToken cancellationToken = default)
    {
        var category = await ValidateAndLoadCategoryAsync(request, cancellationToken);

        var listing = new Listing
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            Category = category,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
        };

        db.Listings.Add(listing);
        await db.SaveChangesAsync(cancellationToken);

        return listing;
    }

    public async Task<Listing> UpdateAsync(int listingId, string requestingUserId, ListingEditRequest request, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings.FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), listingId);

        if (listing.OwnerId != requestingUserId)
        {
            throw new ForbiddenException("Only the listing owner may edit this listing.");
        }

        var category = await ValidateAndLoadCategoryAsync(request, cancellationToken);

        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.Price = request.Price;
        listing.CategoryId = request.CategoryId;
        listing.Category = category;

        await db.SaveChangesAsync(cancellationToken);

        return listing;
    }

    public async Task DeleteAsync(int listingId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        var listing = await db.Listings
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Listing), listingId);

        if (listing.OwnerId != requestingUserId)
        {
            throw new ForbiddenException("Only the listing owner may delete this listing.");
        }

        foreach (var image in listing.Images)
        {
            await imageStorage.DeleteAsync(image.Path, cancellationToken);
        }

        db.Listings.Remove(listing);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> ValidateAndLoadCategoryAsync(ListingEditRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title is required.");
        }
        else if (request.Title.Trim().Length > MaxTitleLength)
        {
            errors.Add($"Title must be {MaxTitleLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Description is required.");
        }
        else if (request.Description.Trim().Length > MaxDescriptionLength)
        {
            errors.Add($"Description must be {MaxDescriptionLength} characters or fewer.");
        }

        if (request.Price is < 0)
        {
            errors.Add("Price must not be negative.");
        }

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category is null)
        {
            errors.Add("Category is invalid.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return category!;
    }
}
