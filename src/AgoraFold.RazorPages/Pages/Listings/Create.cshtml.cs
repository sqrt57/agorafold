using System.ComponentModel.DataAnnotations;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgoraFold.RazorPages.Pages.Listings;

[Authorize]
public class CreateModel(IListingService listingService, IListingImageService listingImageService, ICategoryService categoryService) : AgoraFoldPageModel
{
    [BindProperty]
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [BindProperty]
    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = "";

    [BindProperty]
    [Range(0, 100_000_000)]
    public decimal? Price { get; set; }

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [BindProperty]
    [Display(Name = "Images")]
    public List<IFormFile>? Images { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await BuildCategorySelectListAsync(null, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Categories = await BuildCategorySelectListAsync(CategoryId, cancellationToken);
            return Page();
        }

        Core.Entities.Listing listing;
        try
        {
            listing = await listingService.CreateAsync(
                CurrentUserId,
                new ListingEditRequest(Title, Description, Price, CategoryId),
                cancellationToken);
        }
        catch (AgoraFold.Core.Exceptions.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            Categories = await BuildCategorySelectListAsync(CategoryId, cancellationToken);
            return Page();
        }

        if (Images is { Count: > 0 })
        {
            var uploads = Images
                .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
                .ToList();

            try
            {
                await listingImageService.AddImagesAsync(listing.Id, CurrentUserId, uploads, cancellationToken);
            }
            catch (AgoraFold.Core.Exceptions.ValidationException ex)
            {
                TempData["ImageErrors"] = string.Join(" ", ex.Errors);
                return RedirectToPage("/Listings/Edit", new { id = listing.Id });
            }
        }

        return RedirectToPage("/Listings/Details", new { id = listing.Id });
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategorySelectListAsync(int? selectedId, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selectedId)).ToList();
    }
}
