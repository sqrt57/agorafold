namespace AgoraFold.WebApi.Models.Conversations;

public sealed record ConversationSummaryResponse(
    int Id,
    int ListingId,
    string ListingTitle,
    string OtherPartyDisplayName,
    string? LastMessageBody,
    DateTime LastActivityAt,
    bool LastMessageIsMine);
