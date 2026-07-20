namespace AgoraFold.WebApi.Models.Listings;

public sealed record ListingUpdateRequest(string Title, string Description, decimal? Price, int CategoryId);
