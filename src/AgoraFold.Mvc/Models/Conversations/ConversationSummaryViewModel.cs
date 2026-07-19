namespace AgoraFold.Mvc.Models.Conversations;

public record ConversationSummaryViewModel(
    int Id,
    int ListingId,
    string ListingTitle,
    string OtherPartyDisplayName,
    string? LastMessagePreview,
    DateTime LastMessageAt,
    bool IsLastMessageMine);
