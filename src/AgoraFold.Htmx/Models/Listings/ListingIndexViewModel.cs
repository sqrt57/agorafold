using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgoraFold.Htmx.Models.Listings;

public class ListingIndexViewModel
{
    public required IReadOnlyList<ListingSummaryViewModel> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasPreviousPage { get; init; }
    public required bool HasNextPage { get; init; }
    public int? CategoryId { get; init; }
    public string? SearchTerm { get; init; }
    public required IReadOnlyList<SelectListItem> Categories { get; init; }
}
