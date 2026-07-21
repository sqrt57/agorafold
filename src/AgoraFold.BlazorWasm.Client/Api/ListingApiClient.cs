using System.Globalization;
using System.Net.Http.Headers;
using AgoraFold.BlazorWasm.Client.Api.Dto.Listings;

namespace AgoraFold.BlazorWasm.Client.Api;

public sealed class ListingApiClient(ApiClient api)
{
    public Task<PagedListingResponse> BrowseAsync(int? categoryId, string? searchTerm, int page, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"page={page}" };
        if (categoryId is not null)
        {
            query.Add($"categoryId={categoryId}");
        }
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        }

        return api.GetAsync<PagedListingResponse>($"api/listings?{string.Join('&', query)}", cancellationToken);
    }

    public Task<ListingDetailResponse> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<ListingDetailResponse>($"api/listings/{id}", cancellationToken);

    public Task<IReadOnlyList<ListingSummaryResponse>> GetMineAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<IReadOnlyList<ListingSummaryResponse>>("api/listings/mine", cancellationToken);

    public async Task<ListingDetailResponse> CreateAsync(
        string title, string description, decimal? price, int categoryId,
        IReadOnlyList<ListingFileUpload> images, CancellationToken cancellationToken = default)
    {
        // Must actually await here (not just return the inner Task): a bare `using var x = ...;
        // return SomeAsyncCall(x);` disposes x synchronously the moment this method returns the
        // Task, not after that Task completes - which disposed the multipart content (and the
        // image streams it wraps) before the HTTP send had actually read them.
        using var content = BuildListingFormContent(title, description, price, categoryId, images);
        return await api.PostFormAsync<ListingDetailResponse>("api/listings", content, cancellationToken);
    }

    public Task<ListingDetailResponse> UpdateAsync(int id, ListingUpdateRequest request, CancellationToken cancellationToken = default) =>
        api.PutJsonAsync<ListingUpdateRequest, ListingDetailResponse>($"api/listings/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        api.DeleteAsync($"api/listings/{id}", cancellationToken);

    public async Task<IReadOnlyList<ListingImageResponse>> AddImagesAsync(int id, IReadOnlyList<ListingFileUpload> images, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        AddImages(content, images);
        return await api.PostFormAsync<IReadOnlyList<ListingImageResponse>>($"api/listings/{id}/images", content, cancellationToken);
    }

    public Task DeleteImageAsync(int id, int imageId, CancellationToken cancellationToken = default) =>
        api.DeleteAsync($"api/listings/{id}/images/{imageId}", cancellationToken);

    private static MultipartFormDataContent BuildListingFormContent(
        string title, string description, decimal? price, int categoryId, IReadOnlyList<ListingFileUpload> images)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(title), "Title" },
            { new StringContent(description), "Description" },
            { new StringContent(categoryId.ToString(CultureInfo.InvariantCulture)), "CategoryId" },
        };

        if (price is not null)
        {
            content.Add(new StringContent(price.Value.ToString(CultureInfo.InvariantCulture)), "Price");
        }

        AddImages(content, images);
        return content;
    }

    private static void AddImages(MultipartFormDataContent content, IReadOnlyList<ListingFileUpload> images)
    {
        foreach (var image in images)
        {
            var fileContent = new StreamContent(image.Content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
            content.Add(fileContent, "Images", image.FileName);
        }
    }
}
