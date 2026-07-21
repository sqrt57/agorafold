namespace AgoraFold.BlazorWasm.Client.Api.Dto.Conversations;

public sealed record ConversationThreadResponse(int Id, int ListingId, string ListingTitle, IReadOnlyList<ConversationMessageResponse> Messages);
