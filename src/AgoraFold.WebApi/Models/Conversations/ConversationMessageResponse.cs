namespace AgoraFold.WebApi.Models.Conversations;

public sealed record ConversationMessageResponse(int Id, string SenderDisplayName, string Body, DateTime SentAt, bool IsMine);
