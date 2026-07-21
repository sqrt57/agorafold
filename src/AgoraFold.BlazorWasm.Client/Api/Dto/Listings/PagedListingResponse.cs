namespace AgoraFold.BlazorWasm.Client.Api.Dto.Listings;

public sealed record PagedListingResponse(
    IReadOnlyList<ListingSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
