using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.RazorPages.Pages.Listings;

public class DetailsModel(IListingService listingService, IConversationService conversationService) : AgoraFoldPageModel
{
    public int Id { get; private set; }

    public string Title { get; private set; } = "";

    public string Description { get; private set; } = "";

    public decimal? Price { get; private set; }

    public string CategoryName { get; private set; } = "";

    public string OwnerDisplayName { get; private set; } = "";

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<string> Images { get; private set; } = [];

    public bool IsOwner { get; private set; }

    public bool CanMessage { get; private set; }

    public async Task OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);
        var isOwner = User.Identity?.IsAuthenticated == true && listing.OwnerId == CurrentUserId;

        Id = listing.Id;
        Title = listing.Title;
        Description = listing.Description;
        Price = listing.Price;
        CategoryName = listing.Category.Name;
        OwnerDisplayName = listing.Owner.DisplayName;
        CreatedAt = listing.CreatedAt;
        Images = listing.Images.Select(i => $"/uploads/listings/{i.Path}").ToList();
        IsOwner = isOwner;
        CanMessage = User.Identity?.IsAuthenticated == true && !isOwner;
    }

    public async Task<IActionResult> OnPostStartAsync(int listingId, CancellationToken cancellationToken)
    {
        var conversation = await conversationService.StartConversationAsync(listingId, CurrentUserId, cancellationToken);
        return RedirectToPage("/Conversations/Details", new { id = conversation.Id });
    }
}
