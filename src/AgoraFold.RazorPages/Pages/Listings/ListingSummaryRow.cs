namespace AgoraFold.RazorPages.Pages.Listings;

public record ListingSummaryRow(int Id, string Title, decimal? Price, string CategoryName, string? ThumbnailUrl, DateTime CreatedAt);
