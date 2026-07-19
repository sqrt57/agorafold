namespace AgoraFold.Mvc.Models.Conversations;

public record ConversationMessageViewModel(string SenderDisplayName, string Body, DateTime SentAt, bool IsMine);
