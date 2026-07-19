using System.ComponentModel.DataAnnotations;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.RazorPages.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgoraFold.RazorPages.Pages.Listings;

[Authorize]
public class EditModel(IListingService listingService, IListingImageService listingImageService, ICategoryService categoryService) : AgoraFoldPageModel
{
    [BindProperty]
    public int Id { get; set; }

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
    public List<IFormFile>? Images { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    public IReadOnlyList<ListingImageRow> ExistingImages { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);

        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("You do not own this listing.");
        }

        Id = listing.Id;
        Title = listing.Title;
        Description = listing.Description;
        Price = listing.Price;
        CategoryId = listing.CategoryId;
        Categories = await BuildCategorySelectListAsync(listing.CategoryId, cancellationToken);
        ExistingImages = listing.Images.Select(i => new ListingImageRow(i.Id, $"/uploads/listings/{i.Path}")).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        if (id != Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await ReloadEditGalleryAsync(cancellationToken);
            return Page();
        }

        try
        {
            await listingService.UpdateAsync(
                id,
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

            await ReloadEditGalleryAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage("/Listings/Details", new { id });
    }

    public async Task<IActionResult> OnPostAddImagesAsync(int id, CancellationToken cancellationToken)
    {
        if (Images is not { Count: > 0 })
        {
            TempData["ImageErrors"] = "Choose at least one image to upload.";
            return RedirectToPage("/Listings/Edit", new { id });
        }

        var uploads = Images
            .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
            .ToList();

        try
        {
            await listingImageService.AddImagesAsync(id, CurrentUserId, uploads, cancellationToken);
        }
        catch (AgoraFold.Core.Exceptions.ValidationException ex)
        {
            TempData["ImageErrors"] = string.Join(" ", ex.Errors);
        }

        return RedirectToPage("/Listings/Edit", new { id });
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int listingId, CancellationToken cancellationToken)
    {
        await listingImageService.DeleteImageAsync(imageId, CurrentUserId, cancellationToken);
        return RedirectToPage("/Listings/Edit", new { id = listingId });
    }

    private async Task ReloadEditGalleryAsync(CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(Id, cancellationToken);
        Categories = await BuildCategorySelectListAsync(CategoryId, cancellationToken);
        ExistingImages = listing.Images.Select(i => new ListingImageRow(i.Id, $"/uploads/listings/{i.Path}")).ToList();
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategorySelectListAsync(int? selectedId, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selectedId)).ToList();
    }
}
