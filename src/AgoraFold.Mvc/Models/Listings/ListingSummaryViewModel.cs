namespace AgoraFold.Mvc.Models.Listings;

public record ListingSummaryViewModel(int Id, string Title, decimal? Price, string CategoryName, string? ThumbnailUrl, DateTime CreatedAt);
