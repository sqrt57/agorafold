namespace AgoraFold.WebApi.Models.Listings;

public sealed record ListingDetailResponse(
    int Id,
    string Title,
    string Description,
    decimal? Price,
    int CategoryId,
    string CategoryName,
    string OwnerId,
    string OwnerDisplayName,
    DateTime CreatedAt,
    IReadOnlyList<ListingImageResponse> Images,
    bool IsOwner,
    bool CanMessage,
    IReadOnlyList<string>? ImageErrors = null);
