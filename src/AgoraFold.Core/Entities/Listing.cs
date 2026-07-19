namespace AgoraFold.Core.Entities;

public class Listing
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Price { get; set; }
    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string OwnerId { get; set; } = "";
    public AppUser Owner { get; set; } = null!;

    public ICollection<ListingImage> Images { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}
