namespace AgoraFold.WebApi.Models.Listings;

public sealed record ListingSummaryResponse(int Id, string Title, decimal? Price, string CategoryName, string? ThumbnailUrl, DateTime CreatedAt);
