using System.Security.Claims;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using AgoraFold.Mvc.Models.Listings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgoraFold.Mvc.Controllers;

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
            await ReloadEditGalleryAsync(model, cancellationToken);
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

            await ReloadEditGalleryAsync(model, cancellationToken);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImages(int id, List<IFormFile> images, CancellationToken cancellationToken)
    {
        if (images is not { Count: > 0 })
        {
            TempData["ImageErrors"] = "Choose at least one image to upload.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var uploads = images
            .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
            .ToList();

        try
        {
            await listingImageService.AddImagesAsync(id, CurrentUserId, uploads, cancellationToken);
        }
        catch (ValidationException ex)
        {
            TempData["ImageErrors"] = string.Join(" ", ex.Errors);
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int listingId, CancellationToken cancellationToken)
    {
        await listingImageService.DeleteImageAsync(imageId, CurrentUserId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = listingId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);

        if (listing.OwnerId != CurrentUserId)
        {
            throw new ForbiddenException("You do not own this listing.");
        }

        var vm = new ListingDeleteViewModel(
            listing.Id,
            listing.Title,
            listing.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null);

        return View(vm);
    }

    [Authorize]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        await listingService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return RedirectToAction(nameof(Mine));
    }

    private async Task ReloadEditGalleryAsync(ListingEditViewModel model, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(model.Id, cancellationToken);
        model.Categories = await BuildCategorySelectListAsync(model.CategoryId, cancellationToken);
        model.ExistingImages = listing.Images.Select(i => new ListingImageViewModel(i.Id, $"/uploads/listings/{i.Path}")).ToList();
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildCategorySelectListAsync(int? selectedId, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selectedId)).ToList();
    }
}
