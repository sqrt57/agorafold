using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;
using Microsoft.AspNetCore.Authorization;

namespace AgoraFold.RazorPages.Pages.Listings;

[Authorize]
public class MineModel(IListingService listingService) : AgoraFoldPageModel
{
    public IReadOnlyList<ListingSummaryRow> Listings { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var listings = await listingService.GetMyListingsAsync(CurrentUserId, cancellationToken);

        Listings = listings.Select(l => new ListingSummaryRow(
            l.Id,
            l.Title,
            l.Price,
            l.Category.Name,
            l.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null,
            l.CreatedAt)).ToList();
    }
}
