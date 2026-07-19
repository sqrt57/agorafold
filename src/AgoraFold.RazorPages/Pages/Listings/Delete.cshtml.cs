using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.RazorPages.Pages.Listings;

[Authorize]
public class DeleteModel(IListingService listingService) : AgoraFoldPageModel
{
    public int Id { get; private set; }

    public string Title { get; private set; } = "";

    public string? ThumbnailUrl { get; private set; }

    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);

        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("You do not own this listing.");
        }

        Id = listing.Id;
        Title = listing.Title;
        ThumbnailUrl = listing.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null;
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        await listingService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return RedirectToPage("/Listings/Mine");
    }
}
