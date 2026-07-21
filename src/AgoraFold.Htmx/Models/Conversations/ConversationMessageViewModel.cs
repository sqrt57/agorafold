namespace AgoraFold.Htmx.Models.Conversations;

public record ConversationMessageViewModel(int Id, string SenderDisplayName, string Body, DateTime SentAt, bool IsMine);
