namespace AgoraFold.Core.Entities;

/// <summary>
/// A conversation about one listing, between the listing's owner and one other user (Participant).
/// </summary>
public class Conversation
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }

    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public string ParticipantId { get; set; } = "";
    public AppUser Participant { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = [];
}
