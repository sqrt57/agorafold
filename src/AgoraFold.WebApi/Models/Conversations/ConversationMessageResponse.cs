namespace AgoraFold.WebApi.Models.Conversations;

public sealed record ConversationMessageResponse(string SenderDisplayName, string Body, DateTime SentAt, bool IsMine);
