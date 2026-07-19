namespace AgoraFold.RazorPages.Pages.Conversations;

public record ConversationSummaryRow(
    int Id,
    int ListingId,
    string ListingTitle,
    string OtherPartyDisplayName,
    string? LastMessagePreview,
    DateTime LastMessageAt,
    bool IsLastMessageMine);
