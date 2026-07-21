namespace AgoraFold.Htmx.Models.Listings;

/// <summary>The Edit page's image gallery fragment: existing images plus the upload form, swapped as one unit via `hx-swap="outerHTML"`.</summary>
public record ListingGalleryViewModel(int ListingId, IReadOnlyList<ListingImageViewModel> Images, string? ErrorMessage);
