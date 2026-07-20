namespace AgoraFold.WebApi.Models.Conversations;

public sealed record ConversationThreadResponse(int Id, int ListingId, string ListingTitle, IReadOnlyList<ConversationMessageResponse> Messages);
