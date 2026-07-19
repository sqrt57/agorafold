namespace AgoraFold.Mvc.Models.Conversations;

public class ConversationThreadViewModel
{
    public required int Id { get; init; }
    public required int ListingId { get; init; }
    public required string ListingTitle { get; init; }
    public required IReadOnlyList<ConversationMessageViewModel> Messages { get; init; }
    public required ConversationReplyViewModel Reply { get; set; }
}
