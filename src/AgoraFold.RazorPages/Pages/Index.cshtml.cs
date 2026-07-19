using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Listings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AgoraFold.RazorPages.Pages;

public class IndexModel(IListingService listingService, ICategoryService categoryService) : PageModel
{
    private const int PageSize = 12;

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<ListingSummaryRow> Items { get; private set; } = [];

    public int TotalPages { get; private set; }

    public bool HasPreviousPage { get; private set; }

    public bool HasNextPage { get; private set; }

    public IReadOnlyList<SelectListItem> Categories { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(1, PageNumber);

        var result = await listingService.BrowseAsync(CategoryId, SearchTerm, PageNumber, PageSize, cancellationToken);

        Items = result.Items.Select(l => new ListingSummaryRow(
            l.Id,
            l.Title,
            l.Price,
            l.Category.Name,
            l.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null,
            l.CreatedAt)).ToList();

        PageNumber = result.Page;
        TotalPages = result.TotalPages;
        HasPreviousPage = result.HasPreviousPage;
        HasNextPage = result.HasNextPage;

        var categories = await categoryService.GetAllAsync(cancellationToken);
        Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == CategoryId)).ToList();
    }
}
