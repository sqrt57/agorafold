using System.Security.Claims;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.Htmx.Models.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgoraFold.Htmx.Controllers;

public class ListingsController(IListingService listingService, IListingImageService listingImageService, ICategoryService categoryService) : Controller
{
    private const int PageSize = 12;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, string? searchTerm, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);

        var result = await listingService.BrowseAsync(categoryId, searchTerm, page, PageSize, cancellationToken);

        var vm = new ListingIndexViewModel
        {
            Items = result.Items.Select(l => new ListingSummaryViewModel(
                l.Id,
                l.Title,
                l.Price,
                l.Category.Name,
                l.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null,
                l.CreatedAt)).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage,
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            Categories = await BuildCategorySelectListAsync(categoryId, cancellationToken),
        };

        // A live filter/pagination request from the browse page only swaps the results region -
        // the filter form stays put, so only the fragment needs to go back, not the whole page.
        if (Request.IsHtmx())
        {
            return PartialView("_ListingResults", vm);
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);
        var isOwner = User.Identity?.IsAuthenticated == true && listing.OwnerId == CurrentUserId;

        var vm = new ListingDetailViewModel
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            CategoryName = listing.Category.Name,
            OwnerDisplayName = listing.Owner.DisplayName,
            CreatedAt = listing.CreatedAt,
            Images = listing.Images.Select(i => $"/uploads/listings/{i.Path}").ToList(),
            IsOwner = isOwner,
            CanMessage = User.Identity?.IsAuthenticated == true && !isOwner,
        };

        return View(vm);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var listings = await listingService.GetMyListingsAsync(CurrentUserId, cancellationToken);

        var vm = listings.Select(l => new ListingSummaryViewModel(
            l.Id,
            l.Title,
            l.Price,
            l.Category.Name,
            l.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null,
            l.CreatedAt)).ToList();

        return View(vm);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new ListingCreateViewModel { Categories = await BuildCategorySelectListAsync(null, cancellationToken) });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ListingCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await BuildCategorySelectListAsync(model.CategoryId, cancellationToken);
            return View(model);
        }

        Core.Entities.Listing listing;
        try
        {
            listing = await listingService.CreateAsync(
                CurrentUserId,
                new ListingEditRequest(model.Title, model.Description, model.Price, model.CategoryId),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            model.Categories = await BuildCategorySelectListAsync(model.CategoryId, cancellationToken);
            return View(model);
        }

        if (model.Images is { Count: > 0 })
        {
            var uploads = model.Images
                .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
                .ToList();

            try
            {
                await listingImageService.AddImagesAsync(listing.Id, CurrentUserId, uploads, cancellationToken);
            }
            catch (ValidationException ex)
            {
                TempData["ImageErrors"] = string.Join(" ", ex.Errors);
                return RedirectToAction(nameof(Edit), new { id = listing.Id });
            }
        }

        return RedirectToAction(nameof(Details), new { id = listing.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);

        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("You do not own this listing.");
        }

        var vm = new ListingEditViewModel
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            CategoryId = listing.CategoryId,
            Categories = await BuildCategorySelectListAsync(listing.CategoryId, cancellationToken),
            ExistingImages = listing.Images.Select(i => new ListingImageViewModel(i.Id, $"/uploads/listings/{i.Path}")).ToList(),
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ListingEditViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var listingForGallery = await listingService.GetDetailAsync(model.Id, cancellationToken);
            model.Categories = await BuildCategorySelectListAsync(model.CategoryId, cancellationToken);
            model.ExistingImages = listingForGallery.Images.Select(i => new ListingImageViewModel(i.Id, $"/uploads/listings/{i.Path}")).ToList();
            return View(model);
        }

        try
        {
            await listingService.UpdateAsync(
                id,
                CurrentUserId,
                new ListingEditRequest(model.Title, model.Description, model.Price, model.CategoryId),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var listingForGallery = await listingService.GetDetailAsync(model.Id, cancellationToken);
            model.Categories = await BuildCategorySelectListAsync(model.CategoryId, cancellationToken);
            model.ExistingImages = listingForGallery.Images.Select(i => new ListingImageViewModel(i.Id, $"/uploads/listings/{i.Path}")).ToList();
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Swaps just the `#gallery` fragment (existing images + upload form) - no page reload for adding images.</summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImages(int id, List<IFormFile> images, CancellationToken cancellationToken)
    {
        if (images is not { Count: > 0 })
        {
            return await GalleryFragmentAsync(id, "Choose at least one image to upload.", cancellationToken);
        }

        var uploads = images
            .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
            .ToList();

        string? errorMessage = null;
        try
        {
            await listingImageService.AddImagesAsync(id, CurrentUserId, uploads, cancellationToken);
        }
        catch (ValidationException ex)
        {
            errorMessage = string.Join(" ", ex.Errors);
        }

        return await GalleryFragmentAsync(id, errorMessage, cancellationToken);
    }

    /// <summary>Swaps just the `#gallery` fragment after removing one image - no page reload.</summary>
    [Authorize]
    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int listingId, CancellationToken cancellationToken)
    {
        await listingImageService.DeleteImageAsync(imageId, CurrentUserId, cancellationToken);
        return await GalleryFragmentAsync(listingId, null, cancellationToken);
    }

    /// <summary>
    /// Destructive and leaves the page entirely, so unlike the gallery edits above this doesn't swap a
    /// fragment - it responds with `HX-Redirect` (htmx does a full `window.location` navigation) when
    /// htmx-issued, or a normal redirect otherwise, so it also works with JS/htmx disabled.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteListing(int id, CancellationToken cancellationToken)
    {
        await listingService.DeleteAsync(id, CurrentUserId, cancellationToken);

        var mineUrl = Url.Action(nameof(Mine))!;
        if (Request.IsHtmx())
        {
            Response.Headers["HX-Redirect"] = mineUrl;
            return new EmptyResult();
        }

        return Redirect(mineUrl);
    }

    private async Task<IActionResult> GalleryFragmentAsync(int listingId, string? errorMessage, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(listingId, cancellationToken);

        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("Only the listing owner may manage this listing's images.");
        }

        var vm = new ListingGalleryViewModel(
            listingId,
            listing.Images.Select(i => new ListingImageViewModel(i.Id, $"/uploads/listings/{i.Path}")).ToList(),
            errorMessage);

        return PartialView("_Gallery", vm);
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategorySelectListAsync(int? selectedId, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selectedId)).ToList();
    }
}
