namespace AgoraFold.BlazorWasm.Client.Api.Dto.Listings;

public sealed record ListingUpdateRequest(string Title, string Description, decimal? Price, int CategoryId);
