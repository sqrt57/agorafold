namespace AgoraFold.Core.Entities;

public class ListingImage
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public int SortOrder { get; set; }

    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}
