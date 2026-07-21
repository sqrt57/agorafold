using System.Security.Claims;
using AgoraFold.BlazorWasm.Client.Api.Dto.Listings;
using AgoraFold.BlazorWasm.Filters;
using AgoraFold.BlazorWasm.Models.Listings;
using AgoraFold.Core.Entities;
using AgoraFold.Core.Exceptions;
using AgoraFold.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgoraFold.BlazorWasm.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController(IListingService listingService, IListingImageService listingImageService) : ControllerBase
{
    private const int PageSize = 12;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedListingResponse>> Browse(int? categoryId, string? searchTerm, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);

        var result = await listingService.BrowseAsync(categoryId, searchTerm, page, PageSize, cancellationToken);

        return Ok(new PagedListingResponse(
            result.Items.Select(ToSummary).ToList(),
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDetailResponse>> Details(int id, CancellationToken cancellationToken)
    {
        var listing = await listingService.GetDetailAsync(id, cancellationToken);
        var isOwner = User.Identity?.IsAuthenticated == true && listing.OwnerId == CurrentUserId;

        return Ok(ToDetail(listing, isOwner, canMessage: User.Identity?.IsAuthenticated == true && !isOwner));
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<ListingSummaryResponse>>> Mine(CancellationToken cancellationToken)
    {
        var listings = await listingService.GetMyListingsAsync(CurrentUserId, cancellationToken);
        return Ok(listings.Select(ToSummary).ToList());
    }

    [Authorize]
    [HttpPost]
    [ValidateCsrfToken]
    public async Task<ActionResult<ListingDetailResponse>> Create([FromForm] ListingCreateForm form, CancellationToken cancellationToken)
    {
        var listing = await listingService.CreateAsync(
            CurrentUserId,
            new ListingEditRequest(form.Title, form.Description, form.Price, form.CategoryId),
            cancellationToken);

        IReadOnlyList<string>? imageErrors = null;

        if (form.Images is { Count: > 0 })
        {
            var uploads = form.Images
                .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
                .ToList();

            try
            {
                await listingImageService.AddImagesAsync(listing.Id, CurrentUserId, uploads, cancellationToken);
            }
            catch (ValidationException ex)
            {
                imageErrors = ex.Errors;
            }
        }

        var detail = await listingService.GetDetailAsync(listing.Id, cancellationToken);
        return CreatedAtAction(nameof(Details), new { id = listing.Id }, ToDetail(detail, isOwner: true, canMessage: false) with { ImageErrors = imageErrors });
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ValidateCsrfToken]
    public async Task<ActionResult<ListingDetailResponse>> Update(int id, ListingUpdateRequest request, CancellationToken cancellationToken)
    {
        await listingService.UpdateAsync(
            id,
            CurrentUserId,
            new ListingEditRequest(request.Title, request.Description, request.Price, request.CategoryId),
            cancellationToken);

        var listing = await listingService.GetDetailAsync(id, cancellationToken);
        return Ok(ToDetail(listing, isOwner: true, canMessage: false));
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ValidateCsrfToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await listingService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:int}/images")]
    [ValidateCsrfToken]
    public async Task<ActionResult<IReadOnlyList<ListingImageResponse>>> AddImages(int id, [FromForm] AddImagesForm form, CancellationToken cancellationToken)
    {
        var uploads = form.Images
            .Select(f => new ListingImageUpload(f.OpenReadStream(), f.FileName, f.Length))
            .ToList();

        await listingImageService.AddImagesAsync(id, CurrentUserId, uploads, cancellationToken);

        var listing = await listingService.GetDetailAsync(id, cancellationToken);
        return Ok(listing.Images.Select(ToImage).ToList());
    }

    [Authorize]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [ValidateCsrfToken]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken cancellationToken)
    {
        await listingImageService.DeleteImageAsync(imageId, CurrentUserId, cancellationToken);
        return NoContent();
    }

    private static ListingSummaryResponse ToSummary(Listing listing) =>
        new(
            listing.Id,
            listing.Title,
            listing.Price,
            listing.Category.Name,
            listing.Images.FirstOrDefault() is { } thumbnail ? $"/uploads/listings/{thumbnail.Path}" : null,
            listing.CreatedAt);

    private static ListingDetailResponse ToDetail(Listing listing, bool isOwner, bool canMessage) =>
        new(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Price,
            listing.CategoryId,
            listing.Category.Name,
            listing.OwnerId,
            listing.Owner.DisplayName,
            listing.CreatedAt,
            listing.Images.Select(ToImage).ToList(),
            isOwner,
            canMessage);

    private static ListingImageResponse ToImage(ListingImage image) =>
        new(image.Id, $"/uploads/listings/{image.Path}");
}
