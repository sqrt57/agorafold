namespace AgoraFold.BlazorWasm.Client.Api.Dto.Conversations;

public sealed record ConversationMessageResponse(string SenderDisplayName, string Body, DateTime SentAt, bool IsMine);
