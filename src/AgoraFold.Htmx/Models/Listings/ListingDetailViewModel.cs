namespace AgoraFold.Htmx.Models.Listings;

public class ListingDetailViewModel
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public decimal? Price { get; init; }
    public required string CategoryName { get; init; }
    public required string OwnerDisplayName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<string> Images { get; init; }
    public required bool IsOwner { get; init; }
    public required bool CanMessage { get; init; }
}
